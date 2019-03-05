using System;
using AsyncNet.Tcp.Connection;

namespace AsyncNet.Tcp.Client.Events
{
    public class TcpClientStoppedEventArgs : EventArgs
    {
        public ClientStoppedReason ClientStoppedReason { get; set; }

        public Exception Exception { get; set; }

        public ConnectionCloseReason ConnectionCloseReason { get; set; }
    }
}
