namespace AsyncNet.Tcp.Client
{
    public enum ClientStoppedReason
    {
        InitiatingConnectionTimeout = 0,
        InitiatingConnectionFailure = 1,
        Disconnected = 3,
        RuntimeException = 4
    }
}
