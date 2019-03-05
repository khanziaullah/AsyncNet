using System;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Connection.Events
{
    public class ConnectionEstablishedEventArgs : EventArgs
    {
        public ConnectionEstablishedEventArgs(IRemoteTcpPeer remoteTcpPeer)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
