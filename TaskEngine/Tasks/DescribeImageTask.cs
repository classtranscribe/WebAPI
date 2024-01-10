using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using ClassTranscribeDatabase.Services;
using static ClassTranscribeDatabase.CommonUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
// using SkiaSharp;
using System.IO;
using System.Diagnostics;



// #pragma warning disable CA2007
// https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2007
// We are okay awaiting on a task in the same thread

namespace TaskEngine.Tasks
{
    [SuppressMessage("Microsoft.Performance", "CA1812:MarkMembersAsStatic")] // This class is never directly instantiated
    class DescribeImageTask : RabbitMQTask<string>
    {
     
 
        public DescribeImageTask(RabbitMQConnection rabbitMQ, ILogger<DescribeImageTask> logger)
            : base(rabbitMQ, TaskType.DescribeImage, logger)
        {
           
        }
        /// <summary>Extracts scene descriptions for a video. 
        /// Beware: It is possible to start another scene task while the first one is still running</summary>
        protected async override Task OnConsume(string id, TaskParameters taskParameters, ClientActiveTasks cleanup)
        {
            RegisterTask(cleanup, id); // may throw AlreadyInProgress exception
            GetLogger().LogInformation($"DescribeImageTask({id}): Consuming Task");
            JObject meta = taskParameters.Metadata;
            string captionId = meta["CaptionId"].ToString();
            string imageFile = meta["ImageFile"].ToString();
            string ocrdata = meta["OCRText"].ToString();
            string ocrtext = "";
            try
            {
                JObject ocr = JObject.Parse(ocrdata);
                JArray texts = ocr["text"] as JArray;
                StringBuilder sb = new StringBuilder();
                foreach (var te in texts) {
                    string t = te.ToString();
                    if (string.IsNullOrWhiteSpace(t)) continue;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(t);
                }
                ocrtext = sb.ToString();
            } catch(Exception ex)
            {
                GetLogger().LogError(ex, ex.Message);
            }
            GetLogger().LogInformation($"{captionId}: <{imageFile}> <{ocrtext}>");
            try
            {
                using var _context = CTDbContext.CreateDbContext();
                Caption c = await _context.Captions.FindAsync(captionId);

                if (c == null || !c.HasPlaceHolderText())
                {
                    GetLogger().LogInformation($"Describe Image {id}: Caption Text changed or caption missing");
                    return;
                }
                // string result = $"MOCK AI output: An interesting lecture slide ({captionId}) for image {imageFile} and ocr (\"{ocrtext}\")";
                string description = await DescribeImage(imageFile, ocrtext);
                c.Text = description;
                _context.Update(c);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, ex.Message);
                throw;
            }
            GetLogger().LogInformation($"DescribeImageTask({id}): Complete - end of task");
        }

        /*async Task<SKBitmap> loadImage(string imageFile)
        {
            string baseDir = Globals.appSettings.DATA_DIRECTORY;
            var fullPath = $"{baseDir}/${imageFile}";
            GetLogger().LogInformation($"Opening Image ${fullPath} ...");
            var bytes = await File.ReadAllBytesAsync(fullPath);
            var image = SKBitmap.Decode(bytes);
            GetLogger().LogInformation($"Image ${imageFile} loaded. Dimensions:  ${image.Width} x ${image.Height}");
            return image;
        } */

        async Task<string> DescribeImage(string imagePath, string ocrtext) {
            GetLogger().LogInformation($"DescribeImage Image <${imagePath}> ...");
            if (!File.Exists(imagePath)) { GetLogger().LogError($"DescribeImage. Image file <{imagePath}> does not exist - nothing to do."); return ""; }
            var llavaExec = Globals.appSettings.LLAVA_PATH; //  "/llava/llava-v1.5-7b-q4.llamafile"
            var prompt = Globals.appSettings.LLAVA_PROMPT;
            var cpuCount = Math.Max(1, Environment.ProcessorCount / 2); // don't want hyperthreading  (we are memory bandwidth bound)- and this may report logical not physical cores
                                                                        // besides we dont want monopolize the server
            var llavaArguments = Globals.appSettings.LLAVA_ARGS;
            if (!File.Exists(llavaExec))
            {
                var mesg = $"llava executable: {llavaExec} does not exist - did you install it? Check .env/LLAVA_PATH and taskengine docker mountpoint";
                throw new Exception(mesg);
            }
            if(! llavaArguments.Contains("{imagePath}") || ! llavaArguments.Contains("{prompt}"))
            {
                throw new Exception("LLAVA_ARGS MUST have have {imagePath} and {prompt} placeholders");
            }
            if(String.IsNullOrEmpty(prompt))
            {
                throw new Exception("LLAVA prompt cannot be empty or missing");
            }
            var imagePathEscape = imagePath.Replace("\"", "\\\"");
            var promptEscape = prompt.Replace("\"", "\\\"").Replace("\\n", "\\\\n");
            var args = llavaArguments.Replace("{cpuCount}", $"{cpuCount}").Replace("{prompt}", promptEscape).Replace("{imagePath}", $"{imagePathEscape}");
            if (args.Contains("{") || args.Contains("}") ) {
                throw new Exception("Argument still has a curly brace - unprocessed placeholder? Only {cpuCount|prompt|imagePath} are supported." + args + ". Check LLAVA_ARGS");
            }

            var info = new ProcessStartInfo()
            { //  --escape = Process prompt escapes sequences (\n, \r, \t, \', \", \\)
                FileName = llavaExec,
                Arguments = args, // "--threads 12 --help", // ",
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
            var logOutput = Globals.appSettings.LLAVA_LOG_STREAMS.Contains("out");
            var logError = Globals.appSettings.LLAVA_LOG_STREAMS.Contains("err");

            p.ErrorDataReceived += new DataReceivedEventHandler((src, e) => { errorBuilder.AppendLine(e.Data);
                if (logOutput) GetLogger().LogInformation($"Describe {imagePath} err:${e.Data}");
            });
            p.OutputDataReceived += new DataReceivedEventHandler((src, e) =>{ outputBuilder.AppendLine(e.Data);
                if (logError) GetLogger().LogInformation($"Describe {imagePath} out:${e.Data}");
            });

            var startTime = DateTime.Now;
            GetLogger().LogInformation($"LLAVA Process starting {startTime}");

           
            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            p.StandardInput.Close();
            GetLogger().LogInformation(p.StartInfo.Arguments);

            await p.WaitForExitAsync();
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();

            var endTime = DateTime.Now;
            var processTime = p.TotalProcessorTime;
            GetLogger().LogInformation($"Description complete ({output.Length} characters). ProcessorTime: {processTime} seconds for {endTime-startTime} wallclock seconds");


            p.Close();
            p.Dispose();
            GetLogger().LogInformation($"{imagePath} - Returning. Description:<<{output}>>");
            return output;
        }
    }
}
