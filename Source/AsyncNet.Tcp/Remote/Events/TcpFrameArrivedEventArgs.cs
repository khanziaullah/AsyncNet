namespace AsyncNet.Tcp.Remote.Events
{
    /// <summary>
    /// Contains sender and frame data
    /// </summary>
    public class TcpFrameArrivedEventArgs
    {
        /// <summary>
        /// Creates <see cref="TcpFrameArrivedEventArgs"/> with sender and frame data
        /// </summary>
        /// <param name="remoteTcpPeer">Sender</param>
        /// <param name="frameData">Entire frame data including any headers</param>
        public TcpFrameArrivedEventArgs(IRemoteTcpPeer remoteTcpPeer, byte[] frameData)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.FrameData = frameData;
        }

        /// <summary>
        /// Sender
        /// </summary>
        public IRemoteTcpPeer RemoteTcpPeer { get; }

        /// <summary>
        /// Entire frame data including any headers
        /// </summary>
        public byte[] FrameData { get; }
    }
}
