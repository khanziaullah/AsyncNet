using System;

namespace AsyncNet.Udp.Client.Events
{
    public class UdpClientStartedEventArgs : EventArgs
    {
        public UdpClientStartedEventArgs(string targetHostname, int targetPort)
        {
            this.TargetHostname = targetHostname;
            this.TargetPort = targetPort;
        }

        public string TargetHostname { get; }

        public int TargetPort { get; }
    }
}
