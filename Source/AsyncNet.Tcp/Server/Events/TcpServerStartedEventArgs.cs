using System;
using System.Net;

namespace AsyncNet.Tcp.Server.Events
{
    public class TcpServerStartedEventArgs : EventArgs
    {
        public IPAddress ServerAddress { get; set; }

        public int ServerPort { get; set; }
    }
}
