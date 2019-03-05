using System;

namespace AsyncNet.Udp.Client.Events
{
    public class UdpClientReadyEventArgs : EventArgs
    {
        public UdpClientReadyEventArgs(IAsyncNetUdpClient client)
        {
            this.Client = client;
        }

        public IAsyncNetUdpClient Client { get; }
    }
}
