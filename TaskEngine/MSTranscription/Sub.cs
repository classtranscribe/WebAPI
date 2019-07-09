using System;
using System.Collections.Generic;
using System.IO;

namespace TaskEngine.MSTranscription
{
    public class Sub
    {
        public TimeSpan Begin { get; set; }
        public TimeSpan End { get; set; }
        public string Caption { get; set; }

        public string SrtSubtitle(int Counter)
        {
            String a = "";
            a += Counter + "\n";
            a += string.Format("{0:hh\\:mm\\:ss\\,fff} --> {1:hh\\:mm\\:ss\\,fff}\n", Begin, End);
            a += Caption + "\n\n";
            return a;
        }

        public string WebVTTSubtitle(int Counter)
        {
            String a = "";
            a += Counter + "\n";
            a += string.Format("{0:hh\\:mm\\:ss\\.fff} --> {1:hh\\:mm\\:ss\\.fff}\n", Begin, End);
            a += Caption + "\n\n";
            return a;
        }

        public static List<Sub> GetSubs(Sub sub)
        {
            int subLength = 75;
            List<Sub> subs = new List<Sub>();
            int length = sub.Caption.Length;
            string tempCaption = sub.Caption;
            string caption;
            int newDuration;
            TimeSpan curBegin = sub.Begin;
            TimeSpan curDuration = sub.End.Subtract(sub.Begin);
            TimeSpan curEnd;
            while (tempCaption.Length > subLength)
            {
                newDuration = Convert.ToInt32(subLength * curDuration.TotalMilliseconds / tempCaption.Length);
                int index = tempCaption.IndexOf(' ', subLength);

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
                subs.Add(new Sub
                {
                    Begin = curBegin,
                    End = curEnd,
                    Caption = caption
                });
                Console.WriteLine(caption);
                curBegin = curEnd;
                curDuration = sub.End.Subtract(curBegin);
            }
            if (tempCaption.Length > 0)
            {
                newDuration = Convert.ToInt32(subLength * curDuration.TotalMilliseconds / tempCaption.Length);
                curEnd = curBegin.Add(new TimeSpan(0, 0, 0, 0, newDuration));
                subs.Add(new Sub
                {
                    Begin = curBegin,
                    End = curEnd,
                    Caption = tempCaption
                });
                Console.WriteLine(tempCaption);
                curBegin = curEnd;
                curDuration = sub.End.Subtract(curBegin);
            }
            return subs;
        }

        public static string GenerateSrtFile(List<Sub> subs, String file)
        {
            string srtFile = file.Substring(0, file.IndexOf('.')) + ".srt";
            int Counter = 1;
            String Subtitle = "";
            foreach (Sub sub in subs)
            {
                Subtitle += sub.SrtSubtitle(Counter++);
            }
            WriteSubtitleToFile(Subtitle, srtFile);
            return srtFile;
        }

        public static string GenerateWebVTTFile(List<Sub> subs, String file)
        {
            string srtFile = file.Substring(0, file.IndexOf('.')) + ".vtt";
            int Counter = 1;
            String Subtitle = "WEBVTT\nKind: subtitles\nLanguage: en\n\n";
            foreach (Sub sub in subs)
            {
                Subtitle += sub.WebVTTSubtitle(Counter++);
            }
            WriteSubtitleToFile(Subtitle, srtFile);
            return srtFile;
        }

        public static void WriteSubtitleToFile(string Subtitle, string srtFile)
        {
            try
            {

                //Pass the filepath and filename to the StreamWriter Constructor
                StreamWriter sw = new StreamWriter(srtFile, false, System.Text.Encoding.UTF8);
                //Write a line of text
                sw.WriteLine(Subtitle);
                //Close the file
                sw.Close();
            }
            catch (Exception e1)
            {
                Console.WriteLine("Exception: " + e1.Message);
            }
        }
    }
}
