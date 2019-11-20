using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using TaskEngine.Grpc;

namespace TaskEngine.Tasks
{
    class RefreshBoxTokenTask : RabbitMQTask<Video>
    {
        private void Init(RabbitMQConnection rabbitMQ)
        {
            _rabbitMQ = rabbitMQ;
            queueName = RabbitMQConnection.QueueNameBuilder(CommonUtils.TaskType.RefreshBoxToken, "_1");
        }
        public RefreshBoxTokenTask(RabbitMQConnection rabbitMQ)
        {
            Init(rabbitMQ);
        }
        protected async override Task OnConsume(Video video)
        {
            var path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "refresh.json");
            JObject refresh_json = JObject.Parse(File.ReadAllText(@path));
            String access_token = (String)refresh_json.SelectToken("access_token");
            String refresh_token = (String)refresh_json.SelectToken("refresh_token");
            // get access token using refresh token
            var refresh_client = new RestClient("https://api.box.com/oauth2/token");
            var refresh_request = new RestRequest(Method.POST);
            refresh_request.AddHeader("cache-control", "no-cache");
            refresh_request.AddHeader("Connection", "keep-alive");
            refresh_request.AddHeader("Content-Length", "193");
            refresh_request.AddHeader("Accept-Encoding", "gzip, deflate");
            refresh_request.AddHeader("Host", "api.box.com");
            refresh_request.AddHeader("Postman-Token", "35cc41df-37bc-475b-a330-787ee4dd5647,f83f9780-407a-4d36-96a3-645699f1ae44");
            refresh_request.AddHeader("Cache-Control", "no-cache");
            refresh_request.AddHeader("Accept", "*/*");
            refresh_request.AddHeader("User-Agent", "PostmanRuntime/7.18.0");
            refresh_request.AddParameter("undefined", $"grant_type=refresh_token&client_id=hyqhskag8e4mko8for8dxjdumu37lpyd&client_secret=Byjx7nDHwLgnH8KPF0BkdVXoQOJXpCtd&refresh_token={refresh_token}", ParameterType.RequestBody);
            IRestResponse refresh_response = refresh_client.Execute(refresh_request);
            JObject refresh_content = JObject.Parse(refresh_response.Content);

            // save the refresh token
            using (StreamWriter file = File.CreateText(@path))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                refresh_content.WriteTo(writer);
            }

            access_token = (String)refresh_content.SelectToken("access_token");
        }
    }
}
