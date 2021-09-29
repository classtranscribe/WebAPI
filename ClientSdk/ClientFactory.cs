using System.Net.Http;

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
