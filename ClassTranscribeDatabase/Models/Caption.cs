using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ClassTranscribeDatabase.Models
{
    public enum CaptionType
    {
        TextCaption,
        AudioDescription
    }
    /// <summary>
    /// Each line of caption is stored as a row in the database.
    /// </summary>
    public class Caption : Entity
    {
        public int Index { get; set; }
        public TimeSpan Begin { get; set; }
        public TimeSpan End { get; set; }
        public string Text { get; set; }
        public string TranscriptionId { get; set; }
        public int UpVote { get; set; }
        public int DownVote { get; set; }
        [IgnoreDataMember]
        public virtual Transcription Transcription { get; set; }
        public CaptionType CaptionType { get; set; }
        
        /// <summary>
        /// Convert a line of caption to an srt subtitle format.
        /// </summary>
        public string SrtSubtitle()
        {
            string a = "";
            a += Index + "\n";
            a += string.Format("{0:hh\\:mm\\:ss\\,fff} --> {1:hh\\:mm\\:ss\\,fff}\n", Begin, End);
            a += Text + "\n\n";
            return a;
        }

        /// <summary>
        /// Convert a line of caption to an webVTT subtitle format.
        /// </summary>
        public string WebVTTSubtitle()
        {
            string a = "";
            a += Index + "\n";
            a += string.Format("{0:hh\\:mm\\:ss\\.fff} --> {1:hh\\:mm\\:ss\\.fff}\n", Begin, End);
            a += Text + "\n\n";
            return a;
        }

        /// <summary>
        /// Converts a long line of recognizedSpeech into smaller chunks of Globals.CAPTION_LENGTH characters,
        /// and appends to a list of captions.
        /// </summary>
        /// <param name="captions">A pre-existing list of captions.</param>
        /// <param name="Begin">The beginning time stamp of the recognizedSpeech</param>
        /// <param name="End">The end time stamp of the recognizedSpeech</param>
        /// <param name="recognizedSpeech">Recognized Speech received from the Speech Services API.</param>
        public static List<Caption> AppendCaptions(int captionsCount, TimeSpan Begin, TimeSpan End, string recognizedSpeech)
        {
            List<Caption> captions = new List<Caption>();
            int captionLength = Globals.CAPTION_LENGTH;
            int currCounter = captionsCount;
            string tempCaption = recognizedSpeech;
            string caption;
            int newDuration;
            TimeSpan curBegin = Begin;
            TimeSpan curDuration = End.Subtract(Begin);
            TimeSpan curEnd;
            while (tempCaption.Length > captionLength)
            {
                newDuration = Convert.ToInt32(captionLength * curDuration.TotalMilliseconds / tempCaption.Length);
                int index = tempCaption.IndexOf(' ', captionLength);

                if (index == -1)
                {
                    caption = tempCaption;
                    tempCaption = "";
                }
                else
                {
                    caption = tempCaption.Substring(0, index);
                    tempCaption = tempCaption.Substring(index);
                    tempCaption = tempCaption.Trim();
                }
                curEnd = curBegin.Add(new TimeSpan(0, 0, 0, 0, newDuration));
                captions.Add(new Caption
                {
                    Index = currCounter++,
                    Begin = curBegin,
                    End = curEnd,
                    Text = caption
                });
                curBegin = curEnd;
                curDuration = End.Subtract(curBegin);
            }
            if (tempCaption.Length > 0)
            {
                newDuration = Convert.ToInt32(captionLength * curDuration.TotalMilliseconds / tempCaption.Length);
                curEnd = curBegin.Add(new TimeSpan(0, 0, 0, 0, newDuration));
                captions.Add(new Caption
                {
                    Index = currCounter++,
                    Begin = curBegin,
                    End = curEnd,
                    Text = tempCaption
                });
                curBegin = curEnd;
                curDuration = End.Subtract(curBegin);
            }
            return captions;
        }

        /// <summary>
        /// Generate an srt file from a list of captions.
        /// </summary>
        /// <returns>The path of the generated srt file</returns>
        public static string GenerateSrtFile(List<Caption> captions)
        {
            string srtFile = CommonUtils.GetTmpFile();
            string Subtitle = "";
            foreach (Caption caption in captions)
            {
                Subtitle += caption.SrtSubtitle();
            }
            WriteTextToFile(Subtitle, srtFile);
            return srtFile;
        }

        /// <summary>
        /// Generate a webVTT file from a list of captions.
        /// </summary>
        /// <returns>The path of the generated vtt file</returns>
        public static string GenerateWebVTTFile(List<Caption> captions, string language)
        {
            string vttFile = CommonUtils.GetTmpFile();
            string Subtitle = "WEBVTT Kind: captions; Language: " + language + "\n\n";
            foreach (Caption caption in captions)
            {
                Subtitle += caption.WebVTTSubtitle();
            }
            WriteTextToFile(Subtitle, vttFile);
            return vttFile;
        }

        /// <summary>
        /// Write text to a file.
        /// </summary>
        public static void WriteTextToFile(string text, string file)
        {
            //Pass the filepath and filename to the StreamWriter Constructor
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);
            //Write a line of text
            sw.WriteLine(text);
            //Close the file
            sw.Close();
        }

        /// <summary>
        /// Add offset to every captions in the list.
        /// </summary>

        public static void AddOffset(List<Caption> captions, TimeSpan offset)
        {

        }

    }
}
