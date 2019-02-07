using System;
using AsyncNet.Core.Events;

namespace AsyncNet.Udp.Client.Events
{
    public class UdpClientExceptionEventArgs : ExceptionEventArgs
    {
        public UdpClientExceptionEventArgs(Exception ex) : base(ex)
        {
        }
    }
}
