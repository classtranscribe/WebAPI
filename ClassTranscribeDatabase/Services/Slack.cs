using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase.Services
{
    public class SlackLogger
    {
        private readonly Uri _uri; // null if _appSettings.SLACK_WEBHOOK_URL is unset
        private readonly Encoding _encoding = new UTF8Encoding();
        private readonly ILogger _logger;
        AppSettings _appSettings;


        public SlackLogger(IOptions<AppSettings> appSettings, ILogger<SlackLogger> logger)
        {
            _appSettings = appSettings.Value;
            _logger = logger;

            // ignore
            string url = _appSettings.SLACK_WEBHOOK_URL.Trim();
            if (url.Length > 0 && !url.Contains("<ADD WEBHOOK URL HERE>"))
            {
                _uri = new Uri(url);
            }
        }

        public async Task PostErrorAsync(Exception e, string message, string username = null, string channel = null)
        {
            var obj = new JObject();
            obj["message"] = message;
            obj["Exception"] = e.Message;

            await PostMessageAsync(obj);
        }

        public async Task PostMessageAsync(string text, string username = null, string channel = null)
        {
            Payload payload = new Payload()
            {
                Channel = channel,
                Username = username,
                Text = text
            };

            await PostMessageAsync(payload);
        }

        //Post a message using simple strings
        public async Task PostMessageAsync(JObject jObject, string username = null, string channel = null)
        {
            Payload payload = new Payload()
            {
                Channel = channel,
                Username = username,
                Text = jObject.ToString()
            };

            await PostMessageAsync(payload);
        }

        //Post a message using a Payload object
        public async Task PostMessageAsync(Payload payload)
        {
            
            string payloadJson = JsonConvert.SerializeObject(payload);
            if(_uri == null)
            {
                Console.WriteLine("SLACK_WEBHOOK_URL NOT SET. MESSAGE:");
                Console.WriteLine(payload.Text);
                
                return; // EARLY RETURN TO PREVENT EXCEPTION
            }
            try
            {
                using (WebClient client = new WebClient())
                {
                    NameValueCollection data = new NameValueCollection();
                    data["payload"] = payloadJson;

                    var response = await client.UploadValuesTaskAsync(_uri, "POST", data);

                    //The response text is usually "ok"
                    string responseText = _encoding.GetString(response);
                    if (responseText != "ok")
                    {
                        throw new Exception($"Expected 'ok' response text from slack. Got {responseText}");
                    }
                }
            }
            catch(Exception ex)
            {
               
                Console.WriteLine($"EXCEPTION SENDING SLACK MESSAGE TO '{ _uri.OriginalString }' : {ex.GetType().ToString()} : {ex.Message}");
            }
        }
    }
    //This class serializes into the Json payload required by Slack Incoming WebHooks
    public class Payload
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
