using System;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Connection.Events
{
    public class ConnectionClosedEventArgs : EventArgs
    {
        public ConnectionClosedEventArgs(IRemoteTcpPeer remoteTcpPeer)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }

        public ConnectionCloseReason ConnectionCloseReason { get; set; }

        public Exception ConnectionCloseException { get; set; }
    }
}
