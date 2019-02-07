using System;
using AsyncNet.Core.Remote;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Connection.Events
{
    public class ConnectionClosedEventArgs : EventArgs, ITcpRemoteContext
    {
        public ConnectionClosedEventArgs(IRemoteTcpPeer remoteTcpPeer, ConnectionCloseReason connectionCloseReason)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.ConnectionCloseReason = connectionCloseReason;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }

        public ConnectionCloseReason ConnectionCloseReason { get; }
    }
}
