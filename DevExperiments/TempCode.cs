using ClassTranscribeDatabase;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using CTCommons.Grpc;
using CTCommons.MSTranscription;

namespace CTCommons
{
  class TempCode
    {
        // Deletes all Videos which don't have a file or have an invalid file (size under 1000 bytes)

        private readonly CTDbContext context;
        private readonly MSTranscriptionService _transcriptionService;
        private readonly RpcClient _rpcClient;

        public TempCode(CTDbContext c, MSTranscriptionService transcriptionService, RpcClient rpcClient)
        {
            context = c;
            _transcriptionService = transcriptionService;
            _rpcClient = rpcClient;
        }

        public void Temp()
        {
            TempAsync().GetAwaiter().GetResult();
        }

        private async Task TempAsync()
        {
            // A dummy awaited function call.
            await Task.Delay(0);
            // Add any temporary code.

            Console.WriteLine("Hi");
            
            
            Console.WriteLine("H2");

        }
        // Example code (never called)
        private async Task TestDirectYouTubeChannel()
        {
            JObject json = new JObject();
            json.Add("isChannel", "1");
            var x =await _rpcClient.PythonServerClient.GetYoutubePlaylistRPCAsync(new CTGrpc.PlaylistRequest
            {
                Url = "UCi8e0iOVk1fEOogdfu4YgfA",
                Metadata = new CTGrpc.JsonString() { Json = json.ToString() },


            });
            JArray jArray = JArray.Parse(x.Json);

            Console.WriteLine(x.Json);
        }
    }
}
