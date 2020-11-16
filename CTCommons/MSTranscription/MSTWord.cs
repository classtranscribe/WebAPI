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

        /// <summary>
        /// A brief explanation of the algorithm below.
        /// For a sample caption - “A man and, a woman.”
        ///        The captions are in the form of a tuple - (text, start_time, end_time)
        ///
        /// The word-level captions are - 
        ///(“a”, 00:00:00, 00:00:50) , (“man”, 00:00:50, 00:01:00), (“and”, 00:01:00, 00:01:50), (“a”, 00:01:50, 00:02:00), (“woman”, 00:02:00, 00:02:50)
        ///Notice the lack of capitalization and punctuations in the word-level captions
        ///
        ///The sentence-level captions are - 
        ///(“A man and, a woman.”, 00:00:00, 00:01:50)
        ///
        ///Step 1. Split the sentence level captions into words and add blank words at start and end with the original timing information of the sentence.
        ///
        ///Output -> (START, 00:00:00, 00:00:00), (“A”, ?, ?) , (“man”, ?, ?), (“and,”, ?, ?), (“a”, ?, ?), (“woman.”, ?, ?), (END, 00:02:50, 00:02:50)
        ///
        ///Step 2. Find words that occur only once in both captions and copy the timing information from word-level to sentence-level captions.The string comparisons are done with ignoring case and punctuation.
        ///
        ///Output -> (START, 00:00:00, 00:00:00), (“A”, ?, ?) , (“man”, 00:00:50, 00:01:00), (“and,”, 00:01:00, 00:01:50), (“a”, ?, ?), (“woman.”, 00:02:00, 00:02:50), (END, 00:02:50, 00:02:50)
        ///
        ///Step 3. Extrapolate the timing for the missing words.
        ///
        ///Output -> (START, 00:00:00, 00:00:00), (“A”, 00:00:00, 00:00:50) , (“man”, 00:00:50, 00:01:00), (“and,”, 00:01:00, 00:01:50), (“a”, 00:01:50, 00:02:00), (“woman.”, 00:02:00, 00:02:50), (END, 00:02:50, 00:02:50)
        /// </summary>


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

        /// <summary>
        /// Convert a string to a list of MSTWord
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static List<MSTWord> SentenceToWords(string s)
        {
            return s.Split().ToList().Select(w => new MSTWord
            {
                Word = w,
                Token = Tokenize(w)
            }).ToList();
        }

        /// <summary>
        /// For every word add the number of times it occurs in the list.
        /// </summary>
        public static void AddCount(List<MSTWord> words)
        {
            var grouped = words.GroupBy(w => w.Token).ToList();
            foreach (var word in words)
            {
                word.Count = grouped.Where(g => g.Key == word.Token).First().Count();
            }
        }

        /// <summary>
        /// Compute a lower case and punctuation free representation of the string s
        /// </summary>
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

        // TODO: Rewrite this
        // Remove similar near-duplicate code in ToCaptionEntitiesInterpolate
        public static List<Caption> ToCaptionEntitiesWithWordTiming(int captionsCount, TimeSpan restartOffset, List<MSTWord> words)
        {
            List<Caption> captions = new List<Caption>();
            int thresholdCaptionLength = Globals.CAPTION_LENGTH;
            int currCounter = captionsCount + 1;

            StringBuilder captionText = new StringBuilder();
            TimeSpan? startTime = null;
            TimeSpan endTime = TimeSpan.Zero;

            Action emitCaption = () =>
                   {
                       if (startTime == null)
                       {
                           return; // No caption
                       }

                       captions.Add(new Caption
                       {
                           Index = currCounter++,
                           Begin = (TimeSpan)startTime, // quiet compiler! I promise it will not be null
                           End = endTime,
                           Text = captionText.ToString().Trim()
                       });

                       captionText.Clear();

                       startTime = null;

                   };

            foreach (var word in words)
            {
                int candidateLength = captionText.Length + word.Word.Length + 1;

                if (startTime != null && candidateLength > thresholdCaptionLength)
                {
                    emitCaption();
                }

                startTime ??= new TimeSpan(word.Offset).Add(restartOffset);
                endTime = new TimeSpan(word.Offset + word.Duration).Add(restartOffset);

                captionText.Append(word.Word + " ");

                // Unfortunately this can create single word captions
                // And the preceeding caption end time and the start time of the next caption can hide these in the closed captions
                // This might arise from a difference between the claimed start/end timings of the whole utterance (the start time of the first word)
                // and the rest of the utterance e.g. long offsetDifference = e.Result.OffsetInTicks - wordLevelCaptions.FirstOrDefault().Offset;
                bool endOfSentence = word.Word.Length > 0 && "?!.\"".Contains(word.Word.Last());

                if (endOfSentence)
                {
                    emitCaption();
                }
            }
            // captionText can be non-empty
            emitCaption();

            return captions;
        }
    }
}
