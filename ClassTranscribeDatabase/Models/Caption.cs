using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace ClassTranscribeDatabase.Models
{
    public class MSTWord
    {
        public long Duration { get; set; }
        public long Offset { get; set; }
        public string Word { get; set; }
    }
    public enum CaptionType
    {
        TextCaption,
        AudioDescription
    }
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
        public static int subLength = 40;

        public string SrtSubtitle()
        {
            string a = "";
            a += Index + "\n";
            a += string.Format("{0:hh\\:mm\\:ss\\,fff} --> {1:hh\\:mm\\:ss\\,fff}\n", Begin, End);
            a += Text + "\n\n";
            return a;
        }

        public string WebVTTSubtitle()
        {
            string a = "";
            a += Index + "\n";
            a += string.Format("{0:hh\\:mm\\:ss\\.fff} --> {1:hh\\:mm\\:ss\\.fff}\n", Begin, End);
            a += Text + "\n\n";
            return a;
        }

        public static void AppendCaptions(List<Caption> captions, List<MSTWord> words)
        {
            int currCounter = captions.Count + 1;
            int currLength = 0;
            StringBuilder currSentence = new StringBuilder();
            TimeSpan? startTime = null;
            foreach (MSTWord word in words)
            {
                if (startTime == null)
                {
                    startTime = new TimeSpan(word.Offset);
                }
                currSentence.Append(word.Word + " ");
                currLength += word.Word.Length;

                if (currLength > subLength)
                {
                    captions.Add(new Caption
                    {
                        Index = currCounter++,
                        Begin = startTime ?? new TimeSpan(),
                        End = new TimeSpan(word.Offset + word.Duration),
                        Text = currSentence.ToString().Trim()
                    });
                    currSentence.Clear();
                    currLength = 0;
                    startTime = null;
                }
            }
            if (currLength > 0)
            {
                captions.Add(new Caption
                {
                    Index = currCounter++,
                    Begin = startTime ?? new TimeSpan(),
                    End = new TimeSpan(words[words.Count - 1].Offset + words[words.Count - 1].Duration),
                    Text = currSentence.ToString().Trim()
                });
            }
        }

        public static void AppendCaptions(List<Caption> captions, TimeSpan Begin, TimeSpan End, string Caption)
        {
            int currCounter = captions.Count + 1;
            int length = Caption.Length;
            string tempCaption = Caption;
            string caption;
            int newDuration;
            TimeSpan curBegin = Begin;
            TimeSpan curDuration = End.Subtract(Begin);
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
                newDuration = Convert.ToInt32(subLength * curDuration.TotalMilliseconds / tempCaption.Length);
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
        }

        public static string GenerateSrtFile(List<Caption> captions)
        {
            string srtFile = CommonUtils.GetTmpFile();
            string Subtitle = "";
            foreach (Caption caption in captions)
            {
                Subtitle += caption.SrtSubtitle();
            }
            WriteSubtitleToFile(Subtitle, srtFile);
            return srtFile;
        }

        public static string GenerateWebVTTFile(List<Caption> captions, string language)
        {
            string vttFile = CommonUtils.GetTmpFile();
            string Subtitle = "WEBVTT\nKind: subtitles\nLanguage: " + language + "\n\n";
            foreach (Caption caption in captions)
            {
                Subtitle += caption.WebVTTSubtitle();
            }
            WriteSubtitleToFile(Subtitle, vttFile);
            return vttFile;
        }

        public static void WriteSubtitleToFile(string Subtitle, string file)
        {
            //Pass the filepath and filename to the StreamWriter Constructor
            StreamWriter sw = new StreamWriter(file, false, Encoding.UTF8);
            //Write a line of text
            sw.WriteLine(Subtitle);
            //Close the file
            sw.Close();
        }
    }
}
