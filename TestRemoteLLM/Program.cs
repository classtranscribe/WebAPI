// See https://aka.ms/new-console-template for more information
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Routing.Constraints;

internal class Program
{
    private static async Task<string> DescribeImage(string imagePath)
    {
        if (!File.Exists(imagePath)) { Console.WriteLine($"Invalid image path:<{imagePath}>"); return ""; }
        // Shell examples from https://github.com/Mozilla-Ocho/llamafile
        // ./llava-v1.5-7b-q4.llamafile --temp 0.2 --image lemurs.jpg -e -p '### User: What do you see? \n### Assistant:'

        /* llamafile --temp 0   --image ~/Pictures/lemurs.jpg   -m llava-v1.5-7b-Q4_K.gguf   --mmproj llava-v1.5-7b-mmproj-Q4_0.gguf   -e -p '### User: What do you see?\n### Assistant: ' \
  --silent-prompt 2>/dev/null */

        var execFile = "./llava-v1.5-7b-q4.llamafile";
        var execPath = "E:/downloads/" + execFile;

        if (!File.Exists(execPath)) { Console.WriteLine($"Invalid exec path:<{execPath}>"); return ""; }

        // The first shell example did not explicitly specify the two models; maybe these are the default for llava llamafile?
        var cpuCount = Math.Max(1, Environment.ProcessorCount / 2); // assume hyperthreading - we want physical count because we are memory bandwidth limited

        var llamaOptions = $"--threads {cpuCount} -m llava-v1.5-7b-Q4_K.gguf --mmproj llava-v1.5-7b-mmproj-Q4_0.gguf --temp 0.0 --silent-prompt";

        var prompt = "### User: What do you see in this image?\n### Assistant:"; // add single quotes and -p
        // See https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput?view=net-8.0
        var processArgs = $"{llamaOptions}  --image {imagePath} --escape -p \"{prompt}\"";  //
        var info = new ProcessStartInfo()
        { //  --escape = Process prompt escapes sequences (\n, \r, \t, \', \", \\)
            FileName = execPath,
            Arguments = processArgs, // "--threads 12 --help", // ",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        var errorBuilder = new StringBuilder();
        var outputBuilder = new StringBuilder();
        Process p = new Process()
        {
            StartInfo = info
        };
        bool writeData = false;
        p.ErrorDataReceived += new DataReceivedEventHandler((src, e) =>
            { errorBuilder.AppendLine( e.Data); if(writeData) Console.WriteLine("err:" + e.Data); });
        p.OutputDataReceived += new DataReceivedEventHandler((src, e) =>
            { outputBuilder.AppendLine(e.Data); if(writeData) Console.WriteLine("out:" + e.Data); });
    
        Console.WriteLine("Starting " + DateTime.Now.ToString());

        Process llamaProcess = p;
        if (llamaProcess == null) { Console.WriteLine("Could not create process"); return ""; }

        p.Start();
        p.BeginErrorReadLine();
        p.BeginOutputReadLine();

        llamaProcess.StandardInput.Close();
        Console.WriteLine($"{imagePath}\n{prompt}");
        Console.WriteLine($"{ p.StartInfo.Arguments}");

        await llamaProcess.WaitForExitAsync();
        // var output = await llamaProcess.StandardOutput.ReadToEndAsync();
        var output =outputBuilder.ToString();
        var error = errorBuilder.ToString();

        Console.WriteLine("StandardOutput:");
        Console.WriteLine(output);
        llamaProcess.WaitForExit();

        var processTime = llamaProcess.TotalProcessorTime;
        Console.WriteLine($"ProcessorTime: {processTime}");

        // var err = await llamaProcess.StandardError.ReadToEndAsync();
        Console.WriteLine("StandardError:");
        Console.WriteLine(error);

        llamaProcess.Close();
        llamaProcess.Dispose();
        Console.WriteLine("Ending " + DateTime.Now.ToString());

        return output;
    }
    static async Task Main(string[] args)
    {
        var imageFile = "dieselsubmarine.jpg"; // add --image
        var imagePath = "E:/proj2/testimages/" + imageFile;
        string result = await  DescribeImage(imagePath);
        // var result = "";
        Console.WriteLine("\n\nResult:" + result);

    }

    private static void BadLLamaAPI(string[] args)
    {
        // As of Jan 10, Llama API is broken
        // i) Images are ignored ii) Messing aroud with stop parameter is required,otherwise the server crashes (and ignores future api requests)

        // System.Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_KEYS") ?? defaultKeys
        string fullPath = "../../../" + "dieselsubmarine.jpg"; // ClassTranscribeStudentsUse2020.png";

        var bytes = File.ReadAllBytes(fullPath);
        string imageBytesAsBase64 = Convert.ToBase64String(bytes);
        //string mimetype = "image/png";

        // var image = SKBitmap.Decode(bytes);
        // Console.WriteLine($"Image ${fullPath} loaded. Dimensions:  ${image.Width} x ${image.Height}");

        //var CONTEXT = "A chat between a curious user and an artificial intelligence assistant. The assistant gives helpful, detailed, and polite answers to the user's questions.\n";
        //var prompt = CONTEXT + "What doe image convey [img-1]?";
        //var prompt = "USER:[img-12]Describe the image in detail.\nASSISTANT:";
        var msg = "Describe this image.";
        var prompt = $"A chat between a curious human and an artificial intelligence assistant.The assistant gives helpful, detailed, and polite answers to the human's questions.\nUSER:[img-10]{msg}\nASSISTANT:";

        string model = "llava-v1.5-7b-Q4_K.gguf";
        //  "llava-v1.5-7b-Q4_K.gguf"; /* Verified using unzip -t AND network content*/


        var userRole1 = new JObject { { "role", "user" }, { "content", "Write 2 truthful sentences." } };
        var userRole2 = new JObject { { "role", "user" }, { "content", "tell me history of canada" } };
        var userRole3 = new JObject { { "role", "user" }, { "content", prompt } };

        // https://github.com/Mozilla-Ocho/llamafile/blob/main/llama.cpp/server/README.md#api-endpoints
        // An array of objects to hold base64-encoded image data and its ids to be reference in prompt.
        // You can determine the place of the image in the prompt as in the following: USER:[img-12]Describe the image in detail.\nASSISTANT:
        // In this case, [img-12] will be replaced by the embeddings of the image id 12 in the following image_data array:
        // {..., "image_data": [{"data": "<BASE64_STRING>", "id": 12}]}.

        JObject image12 = new JObject
        {
            {"data", imageBytesAsBase64 }, {"id",10}
        };
        JObject requestJson = new JObject 
            {
            { "model", model}, // "llava-v1.5-7b-Q4_K.GGUF" },
         // { "stop" , null},
            { "mode", "instruct" },
            { "image_data", new JArray { image12 } },        
            { "messages",  new JArray {
            //  systemRole, 
                userRole3
            }
    }
};

        string requestJsonAsString = requestJson.ToString();
       // Console.WriteLine(requestJsonAsString);


        string LLMBASE = "http://localhost:8965/";
        var authKey = "Nokey";

        var clientOptions = new RestClientOptions
        {
            BaseUrl = new Uri(LLMBASE)
        };
        var client = new RestClient(clientOptions, null, null, true /*Enable simple factory */);
        var request = new RestRequest("v1/chat/completions", Method.Post);
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Authorization", $"Bearer {authKey}");
        request.AddJsonBody(requestJsonAsString);

        // request.AddJsonBody(requestJsonAsString);
        // Todo: Are these even required?
        // https://restsharp.dev/usage.html#get-or-post
        // Put or Post ...  Also, the request will be sent as application/x-www-form-urlencoded.

        // In both cases, name and value will automatically be url - encoded.
        //request.AddHeader("content-type", "application/x-www-form-urlencoded");

        RestResponse response = client.Execute(request); // may throw exception
        Console.WriteLine($"ResponseStatus: {response.ResponseStatus}");
        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Content: {response.Content}");
        if (response.Content != null)
        {
            var responseAsJson = JObject.Parse(response.Content);
            var responseContent = responseAsJson["choices"][0]["message"]["content"];
            Console.WriteLine(responseContent);
        }
    }
}