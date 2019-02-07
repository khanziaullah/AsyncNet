using System;

namespace AsyncNet.Tcp.Client.Events
{
    public class TcpClientStartedEventArgs : EventArgs
    {
        public TcpClientStartedEventArgs(string targetHostname, int targetPort)
        {
            this.TargetHostname = targetHostname;
            this.TargetPort = targetPort;
        }

        public string TargetHostname { get; }

        public int TargetPort { get; }
    }
}
