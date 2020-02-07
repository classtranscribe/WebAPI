using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClassTranscribeDatabase
{
    public class SlackLogger
    {
        private readonly Uri _uri;
        private readonly Encoding _encoding = new UTF8Encoding();
        private readonly ILogger _logger;
        AppSettings _appSettings;


        public SlackLogger(IOptions<AppSettings> appSettings, ILogger<SlackLogger> logger)
        {
            _appSettings = appSettings.Value;
            _logger = logger;
            _uri = new Uri("https://hooks.slack.com/services/" + _appSettings.SLACK_WEBHOOK_URL);
        }

        public async Task PostError(Exception e, string message, string username = null, string channel = null)
        {
            var obj = new JObject();
            obj["message"] = message;
            obj["Exception"] = JObject.FromObject(e);

            await PostMessage(obj);
        }

        public async Task PostMessage(string text, string username = null, string channel = null)
        {
            Payload payload = new Payload()
            {
                Channel = channel,
                Username = username,
                Text = text
            };

            await PostMessage(payload);
        }

        //Post a message using simple strings
        public async Task PostMessage(JObject jObject, string username = null, string channel = null)
        {
            Payload payload = new Payload()
            {
                Channel = channel,
                Username = username,
                Text = jObject.ToString()
            };

            await PostMessage(payload);
        }

        //Post a message using a Payload object
        public async Task PostMessage(Payload payload)
        {
            string payloadJson = JsonConvert.SerializeObject(payload);

            using (WebClient client = new WebClient())
            {
                NameValueCollection data = new NameValueCollection();
                data["payload"] = payloadJson;

                var response = await client.UploadValuesTaskAsync(_uri, "POST", data);

                //The response text is usually "ok"
                string responseText = _encoding.GetString(response);
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
