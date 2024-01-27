using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;

using ClassTranscribeDatabase.Models;


namespace ClassTranscribeDatabase.Services
{
    public class BoxAPI
    {
        private readonly SlackLogger _slack;
        private readonly ILogger _logger;
        private BoxClient? _boxClient;
        private DateTimeOffset _lastRefreshed = DateTimeOffset.MinValue;
        private SemaphoreSlim _RefreshSemaphore = new SemaphoreSlim(1, 1); // async-safe mutex to ensure only one thread is refreshing the token at a time

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
                var tmpClient = new Box.V2.BoxClient(config);
                var auth = await tmpClient.Auth.AuthenticateAsync(authCode);

                _logger.LogInformation($"Created Box Tokens Access:({auth.AccessToken.Substring(0, 5)}) Refresh({auth.RefreshToken.Substring(0, 5)})");

                accessToken.Value = auth.AccessToken;
                refreshToken.Value = auth.RefreshToken;
                await _context.SaveChangesAsync();
            }
        }
        /// <summary>
        ///  Updates the accessToken and refreshToken. These keys must already exist in the Dictionary table.
        /// </summary>
        private async Task<BoxClient> RefreshAccessTokenAsync()
        {
            // Only one thread should call this at a time (see semaphore in GetBoxClientAsync)
            try
            {
                _logger.LogInformation($"RefreshAccessTokenAsync: Starting");
                using (var _context = CTDbContext.CreateDbContext())
                {
                    var accessToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_ACCESS_TOKEN).FirstAsync();
                    var refreshToken = await _context.Dictionaries.Where(d => d.Key == CommonUtils.BOX_REFRESH_TOKEN).FirstAsync();
                    var config = new BoxConfig(Globals.appSettings.BOX_CLIENT_ID, Globals.appSettings.BOX_CLIENT_SECRET, new Uri("http://locahost"));
                    var initialAuth = new OAuthSession(accessToken.Value, refreshToken.Value, 3600, "bearer");
                    var initialClient = new BoxClient(config, initialAuth);
                    /// Refresh the access token
                    var auth = await initialClient.Auth.RefreshAccessTokenAsync(initialAuth.AccessToken);
                    /// Create the client again
                    _logger.LogInformation($"RefreshAccessTokenAsync: New Access Token ({auth.AccessToken.Substring(0, 5)}), New Refresh Token ({auth.RefreshToken.Substring(0, 5)})");

                    accessToken.Value = auth.AccessToken;
                    refreshToken.Value = auth.RefreshToken;
                    _lastRefreshed = DateTimeOffset.Now;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"RefreshAccessTokenAsync: Creating New Box Client");
                    var client = new BoxClient(config, auth);
                    return client;
                }
            }
            catch (Box.V2.Exceptions.BoxSessionInvalidatedException e)
            {
                _logger.LogError(e, "RefreshAccessTokenAsync: Box Token Failure.");
                await _slack.PostErrorAsync(e, "RefreshAccessTokenAsync: Box Token Failure.");
                throw;
            }
        }
        /// <summary>
        /// Creates a new box client, after first refreshing the access and refresh token.
        /// </summary>
        public async Task<BoxClient> GetBoxClientAsync()
        {
            try
            {
                await _RefreshSemaphore.WaitAsync(); // // critical section : implementation of an async-safe mutex
                var MAX_AGE_MINUTES = 50;
                var remain = DateTimeOffset.Now.Subtract(_lastRefreshed).TotalMinutes;
                _logger.LogInformation($"GetBoxClientAsync: {remain} minutes since last refresh. Max age {MAX_AGE_MINUTES}.");
                if (_boxClient != null && remain < MAX_AGE_MINUTES)
                {
                    return _boxClient;
                }
                _boxClient = await RefreshAccessTokenAsync();
                _logger.LogInformation($"GetBoxClientAsync: _boxClient updated");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetBoxClientAsync: Box Refresh Failure.");
                throw;
            }
            finally
            {
                _logger.LogInformation($"GetBoxClientAsync: Releasing Semaphore and returning");
                _RefreshSemaphore.Release(1);
            }

            return _boxClient;
        }

    }
}
