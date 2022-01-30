using CTGrpc;
using Grpc.Core;
using System;

namespace ClassTranscribeDatabase.Services
{
    public class RpcClient
    {
        AppSettings _appSettings;
        public PythonServer.PythonServerClient PythonServerClient;
        public RpcClient()
        {
            _appSettings = Globals.appSettings;
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = new Channel(_appSettings.PYTHON_RPC_SERVER, ChannelCredentials.Insecure, new[]{
                      new ChannelOption(ChannelOptions.MaxSendMessageLength , 2*1024*1024),
                      new ChannelOption(ChannelOptions.MaxReceiveMessageLength , 5 *1024*1024)
            });
            PythonServerClient = new PythonServer.PythonServerClient(channel);
        }
    }
}
