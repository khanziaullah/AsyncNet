using System;
using AsyncNet.Core.Events;

namespace AsyncNet.Tcp.Remote.Events
{
    public class RemoteTcpPeerExceptionEventArgs : ExceptionEventArgs
    {
        public RemoteTcpPeerExceptionEventArgs(IRemoteTcpPeer remoteTcpPeer, Exception ex) : base(ex)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }
    }
}
