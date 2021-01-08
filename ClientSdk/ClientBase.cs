using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ClientSdk.Client
{
  
    public abstract class ClientBase
    {
        public string Token { get; set; }

        // Called by implementing swagger client classes
        
        public Task<HttpRequestMessage> CreateHttpRequestMessageAsync(CancellationToken cancellationToken)
        {
            var msg = new HttpRequestMessage();

            if (Token != null)
            {
                msg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            }
            return Task.FromResult(msg);
        }
    }
}
