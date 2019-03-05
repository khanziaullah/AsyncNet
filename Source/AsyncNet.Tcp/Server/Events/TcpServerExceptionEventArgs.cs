using System;
using AsyncNet.Core.Events;

namespace AsyncNet.Tcp.Server.Events
{
    public class TcpServerExceptionEventArgs : ExceptionEventArgs
    {
        public TcpServerExceptionEventArgs(Exception ex) : base(ex)
        {
        }
    }
}
