namespace AsyncNet.Tcp.Remote.Events
{
    public class TcpFrameArrivedEventArgs
    {
        public TcpFrameArrivedEventArgs(IRemoteTcpPeer remoteTcpPeer, byte[] frameData)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.FrameData = frameData;
        }

        public IRemoteTcpPeer RemoteTcpPeer { get; }

        public byte[] FrameData { get; }
    }
}
