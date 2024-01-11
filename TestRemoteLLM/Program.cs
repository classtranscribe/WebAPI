// See https://aka.ms/new-console-template for more information
// using RestSharp;
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

}