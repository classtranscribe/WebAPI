using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase.Services
{
    public class BoxAPI
    {
        private readonly SlackLogger _slack;
        private readonly ILogger _logger;
        public BoxAPI(ILogger<BoxAPI> logger, SlackLogger slack)
        {
            _logger = logger;
            _slack = slack;
        }

        // To generate authCode on a browser open,
        // https://account.box.com/api/oauth2/authorize?client_id=[CLIENT_ID]&response_type=code
        /// <summary>Updates Box accessToken and refreshToken values in the Dictionary table.
        /// Optionally creates these keys if they do not exist.
        /// </summary>
        public async Task CreateAccessTokenAsync(string authCode)
        {
            // This implementation is overly chatty with the database, but we rarely create access tokens so it is not a problem
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
                var client = new Box.V2.BoxClient(config);
                var auth = await client.Auth.AuthenticateAsync(authCode);
                _logger.LogInformation("Created Box Tokens");
                accessToken.Value = auth.AccessToken;
                refreshToken.Value = auth.RefreshToken;
                await _context.SaveChangesAsync();
            }
        }
        /// <summary>
        ///  Updates the accessToken and refreshToken. These keys must already exist in the Dictionary table.
        /// </summary>
        public async Task RefreshAccessTokenAsync()
        {
            try
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
            catch (Box.V2.Exceptions.BoxSessionInvalidatedException e)
            {
                _logger.LogError(e, "Box Token Failure.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw;
            }
        }
        /// <summary>
        /// Creates a new box client, after first refreshing the access and refresh token.
        /// </summary>
        public async Task<BoxClient> GetBoxClientAsync()
        {
            // Todo RefreshAccessTokenAsync could return this information for us; and avoid another trip to the database
            await RefreshAccessTokenAsync();
            BoxClient boxClient;
            using (var _context = CTDbContext.CreateDbContext())
            {
                var accessToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstAsync();
                var refreshToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).FirstAsync();
                var config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
                var auth = new OAuthSession(accessToken.Value, refreshToken.Value, 3600, "bearer");
                boxClient = new Box.V2.BoxClient(config, auth);
            }
            return boxClient;
        }
    }
}
