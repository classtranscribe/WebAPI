using ClassTranscribeDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CTCommons.MSTranscription
{
    /// <summary>
    /// Each object of this class represents an azure subscription key.
    /// </summary>
    public class Key
    {
        public string ApiKey { get; set; }
        public string Region { get; set; }
        /// <summary>
        /// This value indicates the number of tasks that are already using this key currently.
        /// </summary>
        public int Load { get; set; }
    }

    /// <summary>
    /// This class "provides" azure subscription keys for transcription tasks.
    /// It reads its key values from configuration variable "AZURE_SUBSCRIPTION_KEYS"
    /// </summary>
    public class KeyProvider
    {
        private AppSettings _appSettings;
        private List<Key> Keys;
        private HashSet<string> CurrentVideoIds;

        public KeyProvider(AppSettings appSettings)
        {
            _appSettings = appSettings;
            string subscriptionKeys = _appSettings.AZURE_SUBSCRIPTION_KEYS;
            Keys = new List<Key>();
            CurrentVideoIds = new HashSet<string>();
            foreach (string subscriptionKey in subscriptionKeys.Split(';'))
            {
                Keys.Add(new Key
                {
                    ApiKey = subscriptionKey.Split(',')[0],
                    Region = subscriptionKey.Split(',')[1],
                    Load = 0
                });
            }
        }

        /// <summary>
        /// To obtain a new key for a task
        /// </summary>
        public Key GetKey(string videoId)
        {
            if (!CurrentVideoIds.Contains(videoId))
            {
                Key key = Keys.OrderBy(k => k.Load).First();
                key.Load += 1;
                CurrentVideoIds.Add(videoId);
                return key;
            }
            else
            {
                throw new Exception("Video already being transcribed");
            }
        }

        /// <summary>
        /// To release a key that was being used.
        /// </summary>
        public void ReleaseKey(Key key, string videoId)
        {
            Keys.Find(k => k.ApiKey == key.ApiKey).Load -= 1;
            CurrentVideoIds.Remove(videoId);
        }
    }
}
