using System;
using AsyncNet.Udp.Remote;

namespace AsyncNet.Udp.Error.Events
{
    public class UdpSendErrorEventArgs : EventArgs
    {
        public UdpSendErrorEventArgs(
            UdpOutgoingPacket packet,
            int numberOfBytesWrittenToTheSendBuffer,
            Exception exception)
        {
            this.Packet = packet;
            this.NumberOfBytesWrittenToTheSendBuffer = numberOfBytesWrittenToTheSendBuffer;
            this.Exception = exception;
        }

        public UdpOutgoingPacket Packet { get; }

        public int NumberOfBytesWrittenToTheSendBuffer { get; }

        public Exception Exception { get; }

        public UdpSendErrorType SendErrorType
        {
            get
            {
                if (this.Exception != null)
                {
                    return UdpSendErrorType.Exception;
                }
                else
                {
                    return UdpSendErrorType.SocketSendBufferIsFull;
                }
            }
        }
    }
}
