using System;
using System.Net;

namespace AsyncNet.Udp.Server.Events
{
    public class UdpServerStartedEventArgs : EventArgs
    {
        public UdpServerStartedEventArgs(IPAddress serverAddress, int serverPort)
        {
            this.ServerAddress = serverAddress;
            this.ServerPort = serverPort;
        }

        public IPAddress ServerAddress { get; }

        public int ServerPort { get; }
    }
}
