using System;
using AsyncNet.Core.Events;
using AsyncNet.Core.Remote;

namespace AsyncNet.Tcp.Remote.Events
{
    public class RemoteTcpPeerExceptionEventArgs : ExceptionEventArgs, ITcpRemoteContext
    {
        public RemoteTcpPeerExceptionEventArgs(IRemoteTcpPeer remoteTcpPeer, Exception ex) : base(ex)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
