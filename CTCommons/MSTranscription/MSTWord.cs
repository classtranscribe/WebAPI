using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTCommons.MSTranscription
{

    public class MSTWord
    {
        public long Duration { get; set; }
        public long Offset { get; set; }
        public string Word { get; set; }
        public string Token { get; set; }
        public int Count { get; set; }
        public bool Tagged { get; set; }
        public long End { get; set; }

        public static List<MSTWord> WordLevelTimingsToSentenceLevelTimings(string sentenceCaption, List<MSTWord> wordTimingWords)
        {
            List<MSTWord> sentenceTimingWords = SentenceToWords(sentenceCaption);
            wordTimingWords.ForEach(w => w.Token = Tokenize(w.Word));
            AddCount(wordTimingWords);
            AddCount(sentenceTimingWords);


            // Add an empty word at the beginning and end of the sentence.

            sentenceTimingWords.Insert(0, new MSTWord
            {
                Token = "",
                Word = "",
                Offset = wordTimingWords.First().Offset,
                Duration = 0,
                Count = 1,
                Tagged = true
            });
            sentenceTimingWords.Add(new MSTWord
            {
                Token = "",
                Word = "",
                Offset = wordTimingWords.Last().Offset + wordTimingWords.Last().Duration,
                Duration = 0,
                Count = 1,
                Tagged = true
            });



            // Tag all words which occur only once with Duration and Offset.
            sentenceTimingWords.Where(sl_w => sl_w.Count == 1 && wordTimingWords.Where(wl_w => wl_w.Count == 1 && wl_w.Token == sl_w.Token).Any())
                .ToList().ForEach(sl_w =>
                {
                    var wordinWL = wordTimingWords.Where(wl_w => wl_w.Count == 1 && wl_w.Token == sl_w.Token).First();
                    sl_w.Offset = wordinWL.Offset;
                    sl_w.Duration = wordinWL.Duration;
                    sl_w.Tagged = true;
                });


            // Tag all the remaining words
            var lastTaggedIndex = 0;
            for (int i = 1; i < sentenceTimingWords.Count(); i++)
            {
                if (sentenceTimingWords[i].Tagged)
                {
                    if (i - lastTaggedIndex > 1)
                    {
                        // Tag all in between
                        var endOfLastTagged = sentenceTimingWords[lastTaggedIndex].Offset + sentenceTimingWords[lastTaggedIndex].Duration;
                        var startOfCurrentTagged = sentenceTimingWords[i].Offset;

                        // Calculate the time per untagged word.
                        var timePerUntaggedWord = (startOfCurrentTagged - endOfLastTagged) / (i - lastTaggedIndex - 1);
                        var nextOffset = endOfLastTagged;
                        for (int j = lastTaggedIndex + 1; j < i; j++)
                        {
                            sentenceTimingWords[j].Offset = nextOffset;
                            sentenceTimingWords[j].Duration = timePerUntaggedWord;
                            sentenceTimingWords[j].Tagged = true;

                            nextOffset = sentenceTimingWords[j].Offset + sentenceTimingWords[j].Duration;
                        }
                        lastTaggedIndex = i;
                    }
                }
            }

            sentenceTimingWords.ForEach(sl_w => sl_w.End = sl_w.Offset + sl_w.Duration);

            // Removing the extra words added earlier.
            sentenceTimingWords.RemoveAt(0);
            sentenceTimingWords.RemoveAt(sentenceTimingWords.Count - 1);
            return sentenceTimingWords;
        }

        public static List<MSTWord> SentenceToWords(string s)
        {
            return s.Split().ToList().Select(w => new MSTWord
            {
                Word = w,
                Token = Tokenize(w)
            }).ToList();
        }


        public static void AddCount(List<MSTWord> words)
        {
            var grouped = words.GroupBy(w => w.Token).ToList();
            foreach (var word in words)
            {
                word.Count = grouped.Where(g => g.Key == word.Token).First().Count();
            }
        }

        public static string Tokenize(string s)
        {
            var sb = new StringBuilder();
            s = s.ToLowerInvariant();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static void AppendCaptions(List<Caption> captions, List<MSTWord> words)
        {
            int captionLength = Globals.captionLength;
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

                if (currLength > captionLength)
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
    }
}
