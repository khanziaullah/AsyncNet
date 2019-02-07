using System;
using System.Net;

namespace AsyncNet.Tcp.Server.Events
{
    public class TcpServerStartedEventArgs : EventArgs
    {
        public TcpServerStartedEventArgs(IPAddress ipAddress, int serverPort)
        {
            this.ServerAddress = ipAddress;
            this.ServerPort = serverPort;
        }

        public IPAddress ServerAddress { get; }

        public int ServerPort { get; }
    }
}
