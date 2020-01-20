using ClassTranscribeDatabase;
using CTGrpc;
using Grpc.Core;
using System;

namespace TaskEngine.Grpc
{
    class RpcClient
    {
        AppSettings _appSettings;
        public NodeServer.NodeServerClient NodeServerClient;
        public PythonServer.PythonServerClient PythonServerClient;
        public RpcClient()
        {
            _appSettings = Globals.appSettings;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel1 = new Channel(_appSettings.NODE_RPC_SERVER, ChannelCredentials.Insecure, new[]{
                      new ChannelOption(ChannelOptions.MaxSendMessageLength , 2*1024*1024),
                      new ChannelOption(ChannelOptions.MaxReceiveMessageLength , 5 *1024*1024)
            });
            var channel2 = new Channel(_appSettings.PYTHON_RPC_SERVER, ChannelCredentials.Insecure, new[]{
                      new ChannelOption(ChannelOptions.MaxSendMessageLength , 2*1024*1024),
                      new ChannelOption(ChannelOptions.MaxReceiveMessageLength , 5 *1024*1024)
            });
            NodeServerClient = new NodeServer.NodeServerClient(channel1);
            PythonServerClient = new PythonServer.PythonServerClient(channel2);
        }
    }
}
