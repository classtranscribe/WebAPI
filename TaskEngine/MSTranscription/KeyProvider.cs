using ClassTranscribeDatabase;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

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

        public KeyProvider(AppSettings appSettings)
        {
            _appSettings = appSettings;
            string subscriptionKeys = _appSettings.AZURE_SUBSCRIPTION_KEYS;
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

        public Key GetKey()
        {
            Key key = Keys.OrderBy(k => k.Load).First();
            key.Load += 1;
            return key;
        }

        public void ReleaseKey(Key key)
        {
            Keys.Find(k => k.ApiKey == key.ApiKey).Load -= 1;
        }
    }
}
