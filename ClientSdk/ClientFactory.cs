using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ClientSdk.Client
{
    public class ClientFactory
    {
        public static MyClient CreateTestClient(string baseUrl, HttpClient http, string _token)
        {
            return new MyClient(baseUrl, http)
            {
                Token = _token
            };
        }
    }
}
