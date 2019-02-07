namespace AsyncNet.Tcp.Connection
{
    public enum ConnectionCloseReason
    {
        Unknown = 0,
        RemoteShutdown = 1,
        LocalShutdown = 2,
        Timeout = 3,
        ExceptionOccured = 4
    }
}
