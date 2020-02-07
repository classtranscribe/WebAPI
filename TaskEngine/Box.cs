using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TaskEngine
{
    public class Box
    {
        private readonly ILogger _logger;
        public Box(ILogger<Box> logger)
        {
            _logger = logger;
        }

        // To generate authCode on a browser open,
        // https://account.box.com/api/oauth2/authorize?client_id=[CLIENT_ID]&response_type=code
        public async Task CreateAccessTokenAsync(string authCode)
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                if (!await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).AnyAsync())
                {
                    _context.Dictionaries.Add(new Dictionary
                    {
                        Key = CommonUtils.BOX_ACCESS_TOKEN
                    });
                    await _context.SaveChangesAsync();
                }
                if (!await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).AnyAsync())
                {
                    _context.Dictionaries.Add(new Dictionary
                    {
                        Key = CommonUtils.BOX_REFRESH_TOKEN
                    });
                    await _context.SaveChangesAsync();
                }

                var accessToken = _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).First();
                var refreshToken = _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).First();
                var config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
                var client = new BoxClient(config);
                var auth = await client.Auth.AuthenticateAsync(authCode);
                accessToken.Value = auth.AccessToken;
                refreshToken.Value = auth.RefreshToken;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RefreshAccessTokenAsync()
        {
            using (var _context = CTDbContext.CreateDbContext())
            {
                var accessToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstAsync();
                var refreshToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).FirstAsync();
                var config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
                var auth = new OAuthSession(accessToken.Value, refreshToken.Value, 3600, "bearer");
                var client = new BoxClient(config, auth);
                /// Try to refresh the access token
                auth = await client.Auth.RefreshAccessTokenAsync(auth.AccessToken);
                /// Create the client again
                client = new BoxClient(config, auth);
                _logger.LogInformation("Refreshed Tokens");
                accessToken.Value = auth.AccessToken;
                refreshToken.Value = auth.RefreshToken;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<BoxClient> GetBoxClientAsync()
        {
            // Refresh token before each use.
            await RefreshAccessTokenAsync();
            BoxClient boxClient;
            using (var _context = CTDbContext.CreateDbContext())
            {
                var accessToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstAsync();
                var refreshToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).FirstAsync();
                var config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
                var auth = new OAuthSession(accessToken.Value, refreshToken.Value, 3600, "bearer");
                boxClient = new BoxClient(config, auth);
            }
            return boxClient;
        }
    }
}
