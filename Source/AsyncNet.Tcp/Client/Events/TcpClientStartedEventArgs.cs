using System;

namespace AsyncNet.Tcp.Client.Events
{
    public class TcpClientStartedEventArgs : EventArgs
    {
        public string TargetHostname { get; set; }

        public int TargetPort { get; set; }
    }
}
