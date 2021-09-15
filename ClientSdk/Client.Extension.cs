using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Net.Http;
using System.Reflection;


namespace ClientSdk.Client
{
  public partial class MyClient
    {
        partial void ProcessResponse(HttpClient request, HttpResponseMessage response)
        {
            // Normally the response statuscode of successful create request is 201
            // But classTranscribe apis declare all reponse statuscode of successful api calls to be 200 
            // To avoid conflict, convert all 201 status code to be 200 before comparing
            if ((int) response.StatusCode == 201) 
            {
                response.StatusCode = (HttpStatusCode)200;
            }
        }
        public class SwaggerContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
             
                var jsonProperty = base.CreateProperty(member, memberSerialization);
                jsonProperty.NullValueHandling = NullValueHandling.Include;
                return jsonProperty;

            }
        }

        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new SwaggerContractResolver();
            settings.NullValueHandling = NullValueHandling.Include;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.Include;

        }

     
    }
}
