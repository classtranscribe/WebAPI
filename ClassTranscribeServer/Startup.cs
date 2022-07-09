using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using ClassTranscribeServer.Authorization;
using ClassTranscribeServer.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ClassTranscribeServer
{
    public class Startup
    {
        public Startup(IOptions<AppSettings> appSettings)
        {
            if (appSettings != null)
            {
                Globals.appSettings = appSettings.Value;
            }
        }

        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });
            services.AddDbContext<CTDbContext>(options =>
                options.UseLazyLoadingProxies().UseNpgsql(CTDbContext.ConnectionStringBuilder()));

            //// ===== Add Identity ========
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 4;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            }).AddRoles<IdentityRole>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddEntityFrameworkStores<CTDbContext>()
                .AddDefaultTokenProviders();
            // ===== Add Jwt Authentication ========
            var jwt_issuer = "https://" + Globals.appSettings.HOST_NAME;
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwt_issuer,
                        ValidAudience = jwt_issuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Globals.appSettings.JWT_KEY)),
                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });

            services.AddMvc().AddNewtonsoftJson().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // In case of multipart
            });

            // Authorization handlers.
            services.AddScoped<IAuthorizationHandler,
                                  ReadOfferingAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler,
                                  UpdateOfferingAuthorizationHandler>();

            // Configure your policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Globals.POLICY_UPDATE_OFFERING, policy =>
                    policy.Requirements.Add(new UpdateOfferingRequirement()));
                options.AddPolicy(Globals.POLICY_READ_OFFERING,
                  policy => policy.AddRequirements(new ReadOfferingRequirement()));
            });
           

            // Register the Swagger generator, defining 1 or more Swagger documents
            // new ApiKeyScheme { };
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ClassTranscribeServer API",
                    Description ="An accessible video platform server. Internal Ref: 0x14cd. See ClassTranscribeServer/Controllers for implementation (https://github.com/classtranscribe/WebAPI/tree/master/ClassTranscribeServer/Controllers)"
                });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
                    "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: Bearer 12345abcde",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                    {
                        new OpenApiSecurityScheme{
                            Reference = new OpenApiReference{
                                Id = "Bearer", //The name of the previously defined security scheme.
                                Type = ReferenceType.SecurityScheme
                            }
                        },new List<string>()
                    }
                });
                c.SchemaFilter<SwaggerSchemaFilter>();
            });
            //services.AddApplicationInsightsTelemetry(Globals.appSettings.APPLICATION_INSIGHTS_KEY);
            services.AddScoped<RabbitMQConnection>();
            services.AddScoped<WakeDownloader>();
            services.AddScoped<Seeder>();
            services.AddScoped<UserUtils>();
            services.AddScoped<CaptionQueries>();
            services.AddSingleton(_ => new PhysicalFileProvider(Globals.appSettings.DATA_DIRECTORY));

            // Configure ElasticSearch client
            if (!string.IsNullOrEmpty(Globals.appSettings.ES_CONNECTION_ADDR))
            {
                var connection = new Uri(Globals.appSettings.ES_CONNECTION_ADDR);
                using var settings = new ConnectionSettings(connection);
                var client = new ElasticClient(settings);
                services.AddSingleton<IElasticClient>(client);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Seeder seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
            app.UseCors(MyAllowSpecificOrigins);
            // app.UseHttpsRedirection();
            app.UseAuthentication();
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
   
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = "swag";
            });

            app.UseRouting();
            
            app.UseAuthorization();
        /* we havent configured any middleware compression prior to UseStaticFiles() 
                 * and compression doesnt seem to be used when I examined the response headers of an mp4 request
                 * but let's explicitly turn it off because we do not want to waste any time tryng to 
                 * compress images, audio and video content - which is 99.9% of our served content
                 * See https://gunnarpeipman.com/aspnet-core-compress-gzip-brotli-content-encoding/ 
                */
            //if(false) {
            //    //notused 
            //    app.UseStaticFiles(new StaticFileOptions
            //    {
            //        ServeUnknownFileTypes = true,
            //        FileProvider = new PhysicalFileProvider(Globals.appSettings.DATA_DIRECTORY),
            //        RequestPath = "/data",
            //        HttpsCompression = HttpsCompressionMode.DoNotCompress
            //        // OnPrepareResponse= prepareStaticFileResponse
            //    });
            //}

            app.UseEndpoints(endpoints =>
            {
//                endpoints.MapControllerRoute(name: "StaticFile", pattern: "data/{**id}",defaults: new { controller = "StaticFile", action = "GetFile"});

                endpoints.MapDefaultControllerRoute().RequireAuthorization();      
            });

            if (seeder != null)
            {
                seeder.Seed();
            }
        }
    }
}
