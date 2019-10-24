using ClassTranscribeDatabase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using ClassTranscribeDatabase.Models;

namespace TaskEngine.MSTranscription
{
    public class Key
    {
        public string ApiKey { get; set; }
        public string Region { get; set; }
        public int Load { get; set; }
        public Semaphore Semaphore { get; set; }
    }
    public class KeyProvider
    {
        private AppSettings _appSettings;
        private List<Key> Keys;
        private HashSet<string> currentVideoIds;

        public KeyProvider(AppSettings appSettings)
        {
            _appSettings = appSettings;
            string subscriptionKeys = _appSettings.AZURE_SUBSCRIPTION_KEYS;
            Keys = new List<Key>();
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

        public Key GetKey(string videoId)
        {
            if(!currentVideoIds.Contains(videoId))
            {
                Key key = Keys.OrderBy(k => k.Load).First();
                key.Load += 1;
                currentVideoIds.Add(videoId);
                return key;
            } 
            else
            {
                throw new Exception("Video already being transcribed");
            }
        }

        public void ReleaseKey(Key key, string videoId)
        {
            Keys.Find(k => k.ApiKey == key.ApiKey).Load -= 1;
            currentVideoIds.Remove(videoId);
        }
    }
}
