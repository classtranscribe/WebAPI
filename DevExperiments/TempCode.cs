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
            
            var filepath = "";
            Key key = new Key
            {
                ApiKey = "",
                Region = ""
            };
            var x = await _transcriptionService.RecognitionWithAudioStreamAsync(filepath, key);
            Console.WriteLine("Hi");
        }
    }
}
