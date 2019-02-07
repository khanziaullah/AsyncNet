using System;
using System.Net;

namespace AsyncNet.Udp.Remote.Events
{
    public class UdpPacketArrivedEventArgs : EventArgs
    {
        public UdpPacketArrivedEventArgs(IPEndPoint remoteEndPoint, byte[] packetData)
        {
            this.RemoteEndPoint = remoteEndPoint;
            this.PacketData = packetData;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public byte[] PacketData { get; }
    }
}
