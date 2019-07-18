using ClassTranscribeDatabase;
using CTGrpc;
using Grpc.Core;
using Microsoft.Extensions.Options;
using System;

namespace TaskEngine.Grpc
{
    class RpcClient
    {
        AppSettings _appSettings;
        public NodeServer.NodeServerClient NodeServerClient;
        public RpcClient()
        {
            _appSettings = CTDbContext.appSettings;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = new Channel(_appSettings.NODE_RPC_SERVER, ChannelCredentials.Insecure, new[]{
                      new ChannelOption(ChannelOptions.MaxSendMessageLength , 2*1024*1024),
                      new ChannelOption(ChannelOptions.MaxReceiveMessageLength , 5 *1024*1024)
            });
            NodeServerClient = new NodeServer.NodeServerClient(channel);
        }
    }
}
