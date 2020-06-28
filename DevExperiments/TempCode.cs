using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CTCommons.Grpc;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons.MSTranscription;
using System.Text.Json;
using System.Text;

namespace CTCommons
{
    class TempCode
    {
        // Deletes all Videos which don't have a file or have an invalid file (size under 1000 bytes)

        private readonly CTDbContext context;
        private readonly MSTranscriptionService _transcriptionService;
        
        public TempCode(CTDbContext c, MSTranscriptionService transcriptionService)
        {
            context = c;
            _transcriptionService = transcriptionService;
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
            
            var filepath = "/Users/petersha/Desktop/fd6b893d-ebce-44a9-8446-18fd2d93f41c_row_.wav";
            Key key = new Key
            {
                ApiKey = "c63829dbd6c7404aa18418cdf3435511",
                Region = "eastus"
            };
            var x = await _transcriptionService.RecognitionWithAudioStreamAsync(filepath, key, TimeSpan.Zero);
            var error_code = x.ErrorCode;
            var lastSucceedTime = x.LastSuccessTime;
            Console.WriteLine(error_code);
            Console.WriteLine(lastSucceedTime.ToString());
        }
    }
}
