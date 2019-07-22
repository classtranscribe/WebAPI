using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TaskEngine.MSTranscription
{
    static class Constants
    {
        public const int FUDGE_START_GAP_MS = 250; // Allow start time of next caption to be a little early, so that endtime-of-last can be exactly starttime of next caption

        public const int NOTABLE_SILENCE_MS = 6000; // A gap of more than this and we'll emit a '[ Silence / Inaudible ]' caption

        public const int MAX_CAPTION_DURATION_MS = 8000; // One caption should not span more than this number of milliseconds

        public const int MAX_INTERWORD_GAP_MS = 1000; // A silence in speech of  more than this and it's time to start a new caption line

        public const int MAX_CAPTION_WORDS = 6; // Limit the number of words in one caption line (except at the very end of the file)

        public const int END_VIDEO_ORPHAN_COUNT = 3; // If we are processing the last few words in the file, then ignore MAX_CAPTION_WORDS and allow a longer last caption line

        public const int ACKNOWLEDGEMENT_PRE_DELAY_MS = 1500; // A short gap between end of captions and displaying acknowledgement

        public const int ACKNOWLEDGEMENT_DURATION_MS = 3500;

        public const string ACKNOWLEDGEMENT_TEXT = "Transcriptions by CSTranscribe, a University of Illinois Digital Accesibility Project";

    }

    public class MSTWord
    {
        public long Duration { get; set; }
        public long Offset { get; set; }
        public string Word { get; set; }
    }

    public class Sub
    {
        public static int subLength = 40;
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

        public static List<Sub> GetSubs(List<MSTWord> words)
        {
            List<Sub> subs = new List<Sub>();

            StringBuilder caption = new StringBuilder();
            int caption_start = 0;
            int caption_end = 0;
            int num_words = words.Count;

            for (int i = 0; i < num_words; i++)
            {
                MSTWord entry = words[i];

                int duration;
                int offset;
                string word;

                try
                {
                // A tick represents one hundred nanoseconds, so convert to milliseconds
                duration = (int)(entry.Duration / 1e4);
                offset = (int)(entry.Offset / 1e4);
                word = entry.Word;
                }
                catch (RuntimeWrappedException e)
                {
                    String s = e.WrappedException as String;
                    if (s != null)
                    {
                        Console.WriteLine(s);
                    }
                    continue;
                }

                bool is_last_few_words = (i >= num_words - Constants.END_VIDEO_ORPHAN_COUNT);

                int gap = offset - caption_end;
                int new_caption_end = offset + duration;

             // Can we just append the word to an existing caption line?
             if ( (caption.Length > 0) &&
                  (new_caption_end - caption_start <= Constants.MAX_CAPTION_DURATION_MS) &&
                  (gap <= Constants.MAX_INTERWORD_GAP_MS) &&
                  (caption.Length < Constants.MAX_CAPTION_WORDS || is_last_few_words) )
                {
                    caption.Append(word + " ");
                    caption_end = new_caption_end;
                    continue;
                }

                // If we get to here then we WILL be starting a new caption, but first check for a long gap and also emit current caption if it exists
                // Have we jumped forward in time? Emit a caption about the long gap in non-transcribed speech
                if (gap > Constants.NOTABLE_SILENCE_MS)
                {
                    Sub current_sub = new Sub
                    {
                        Begin = new TimeSpan(caption_end),
                        End = new TimeSpan(offset),
                        Caption = "[ Silence / Inaudible ]"
                    };
                    subs.Add(current_sub);
                    caption_end = offset;
                }

                if (caption.Length > 0)
                {
                    // Emit current caption (with original end time)
                    Sub current_sub = new Sub
                    {
                        Begin = new TimeSpan(caption_start),
                        End = new TimeSpan(caption_end),
                        Caption = caption.ToString()
                    };
                    subs.Add(current_sub);
                }

                // Reset the caption and start a new one
                caption.Clear();
                caption.Append(word + " ");

                caption_start = offset;
                if (offset - caption_end < Constants.FUDGE_START_GAP_MS)
                {
                    caption_start = caption_end;
                }

                caption_end = new_caption_end;
            }

        // Clean up, we might still be building a caption after processing all of the words
        if (caption.Length > 0)
            {
                // Emit current caption (with original end time)
                Sub current_sub = new Sub
                {
                    Begin = new TimeSpan(caption_start),
                    End = new TimeSpan(caption_end),
                    Caption = caption.ToString()
                };
                subs.Add(current_sub);
            }

        return subs;
        }

        public static List<Sub> GetSubs(Sub sub)
        {
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
