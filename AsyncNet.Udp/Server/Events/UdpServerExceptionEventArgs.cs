using System;
using AsyncNet.Core.Events;

namespace AsyncNet.Udp.Server.Events
{
    public class UdpServerExceptionEventArgs : ExceptionEventArgs
    {
        public UdpServerExceptionEventArgs(Exception ex) : base(ex)
        {
        }
    }
}
