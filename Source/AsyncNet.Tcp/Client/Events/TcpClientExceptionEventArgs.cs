using System;
using AsyncNet.Core.Events;

namespace AsyncNet.Tcp.Client.Events
{
    public class TcpClientExceptionEventArgs : ExceptionEventArgs
    {
        public TcpClientExceptionEventArgs(Exception ex) : base(ex)
        {
        }
    }
}
