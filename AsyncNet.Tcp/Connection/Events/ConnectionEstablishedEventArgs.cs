using System;
using AsyncNet.Core.Remote;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Connection.Events
{
    public class ConnectionEstablishedEventArgs : EventArgs, ITcpRemoteContext
    {
        public ConnectionEstablishedEventArgs(IRemoteTcpPeer remoteTcpPeer)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
