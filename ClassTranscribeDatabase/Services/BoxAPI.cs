using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using ClassTranscribeDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading; // Interlocked

// https://support.box.com/hc/en-us/community/posts/360049144934-Refresh-Token-Expiring-in-1-hour

namespace ClassTranscribeDatabase.Services
{
    public class BoxAPI
    {
        private static int refreshing = 0;
        private readonly SlackLogger _slack;
        private readonly ILogger _logger;
        private readonly BoxConfig _config;
        public BoxAPI(ILogger<BoxAPI> logger, SlackLogger slack)
        {
            _logger = logger;
            _slack = slack;
            _config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
        }

        // Used by Controller/BoxController.cs
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
                var client = new Box.V2.BoxClient(_config);
                var auth = await client.Auth.AuthenticateAsync(authCode);
                _logger.LogInformation("Created Box Tokens");
                //Dictionary accessToken,refreshToken;
                var ( accessToken, refreshToken) = await getOrCreateDatabaseEntries(_context);
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
            _logger.LogInformation("RefreshAccessTokenAsync: Starting");
            try
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    // Dictionary accessToken,refreshToken;
                    var( accessToken, refreshToken) = await getOrCreateDatabaseEntries(_context);

                    var auth = new OAuthSession(accessToken.Value, refreshToken.Value, 3600, "bearer");
                    var client = new BoxClient(_config, auth);
                    /// Try to refresh the access token
                    auth = await client.Auth.RefreshAccessTokenAsync(auth.AccessToken);
                    _logger.LogInformation("RefreshAccessTokenAsync: Complete (RefreshAccessTokenAsync returned)");
                    /// Create the client again
                    client = new BoxClient(_config, auth);
                    if (accessToken.Value != auth.AccessToken)
                    {
                        _logger.LogInformation($"Access Token Changed to ({auth.AccessToken.Substring(4)}...)");
                    }
                    else
                    {
                        _logger.LogInformation($"Access Token Unchanged ({auth.AccessToken.Substring(4)}...)");
                    }
                    if (refreshToken.Value != auth.RefreshToken)
                    {
                        _logger.LogInformation($"Refresh Token Changed to ({auth.RefreshToken.Substring(4)}...");
                    }
                    else
                    {
                        _logger.LogInformation($"Refresh Token Unchanged ({auth.RefreshToken.Substring(4)}...");
                    }

                    accessToken.Value = auth.AccessToken;
                    refreshToken.Value = auth.RefreshToken;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("RefreshAccessTokenAsync: Complete (database updated)");
                }
            }
            catch (Box.V2.Exceptions.BoxSessionInvalidatedException e)
            {
                _logger.LogError(e, "Box Token Failure.");
                await _slack.PostErrorAsync(e, "Box Token Failure.");
                throw;
            }
            _logger.LogInformation("RefreshAccessTokenAsync: returning");

        }
        public async Task<(Dictionary,Dictionary)> getOrCreateDatabaseEntries(CTDbContext context)
        {
            // sanity check- expect 0 or 1 entries for key
            if(await context.Dictionaries.Where(d=>d.Key == CommonUtils.BOX_ACCESS_TOKEN).CountAsync() >1 
            || await context.Dictionaries.Where(d=>d.Key == CommonUtils.BOX_REFRESH_TOKEN).CountAsync() >1 )
            {   // should never happen
                var badEntries = context.Dictionaries.Where(d=>d.Key == CommonUtils.BOX_ACCESS_TOKEN || d.Key == CommonUtils.BOX_REFRESH_TOKEN);
                context.Dictionaries.RemoveRange(badEntries);
                await context.SaveChangesAsync();
            }
            var changed = false;

            var accessToken = await context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstOrDefaultAsync();
            var refreshToken = await context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).FirstOrDefaultAsync();

            if (accessToken == null)
            {
                accessToken = new Dictionary
                {
                    Key = CommonUtils.BOX_ACCESS_TOKEN
                };
                context.Dictionaries.Add(accessToken);
                changed = true;
            }
            if (refreshToken == null)
            {
                refreshToken = new Dictionary
                {
                    Key = CommonUtils.BOX_REFRESH_TOKEN
                };
                context.Dictionaries.Add(refreshToken);
                changed = true;
            }
            if (changed)
            {
                await context.SaveChangesAsync();
            }
            return (accessToken, refreshToken);
        }

        /// <summary>
        /// Creates a new box client, after first refreshing the access and refresh token.
        /// </summary>
        public async Task<BoxClient> GetBoxClientAsync()
        {
            // Todo RefreshAccessTokenAsync could return this information for us; and avoid another trip to the database
            int attempt = 1;
            int maxAttempt = 10;
            while (attempt < maxAttempt)
            {
                using (var _context = CTDbContext.CreateDbContext())
                {
                    _logger.LogInformation($"GetBoxClientAsync: Attempt {attempt} of {maxAttempt} to get valid client");
                    // var accessToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstOrDefaultAsync();
                    //Dictionary accessToken,refreshToken;
                    
                    var (accessToken, refreshToken) = await getOrCreateDatabaseEntries(_context);
                    
                    if (string.IsNullOrEmpty(accessToken.Value))
                    {
                        _logger.LogInformation($"GetBoxClientAsync: Attempting box client using access token ({accessToken.Value.Substring(4)}...");
                        try
                        {
                            var auth = new OAuthSession(accessToken.Value, "", 3600, "bearer");

                            var client = new BoxClient(_config, auth);
                            _logger.LogInformation($"GetBoxClientAsync: Attempt {attempt} returning client using existing access token");
                            return client; // Normal return here
                        }
                        catch (Exception e)
                        {
                            _logger.LogInformation(e, "GetBoxClientAsync: Existing access token is invalid");
                        }
                    }
                }
                if (refreshing > 0)
                {
                    var sleep = 5 + 5 * attempt;
                    _logger.LogInformation($"GetBoxClientAsync: refresh in progress - Sleeping {sleep} seconds - Give time for another thread to refresh the token before retrying");
                    await Task.Delay(sleep * 1000);
                }
                else
                {
                    Interlocked.Increment(ref refreshing); // threadsafe refreshing ++;
                    _logger.LogInformation($"GetBoxClientAsync: Calling RefreshAccessTokenAsync");
                    await RefreshAccessTokenAsync();
                    Interlocked.Decrement(ref refreshing);
                }
            }
            _logger.LogError("Failed to authenticate with Box");
            throw new Exception("Failed to authenticate with Box");
            // BoxClient boxClient;
            // using (var _context = CTDbContext.CreateDbContext())
            // {
            //     var accessToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstAsync();
            //     var refreshToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).FirstAsync();
            //     var config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
            //     var auth = new OAuthSession(accessToken.Value, refreshToken.Value, 3600, "bearer");
            //     boxClient = new Box.V2.BoxClient(config, auth);
            // }
            // return boxClient;
        }
    }
}

