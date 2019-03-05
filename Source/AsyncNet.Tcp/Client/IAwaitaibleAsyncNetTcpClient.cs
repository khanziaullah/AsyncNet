using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Client
{
    public interface IAwaitaibleAsyncNetTcpClient : IDisposable
    {
        AsyncNetTcpClient Client { get; }

        Task<IAwaitaibleRemoteTcpPeer> ConnectAsync();
        Task<IAwaitaibleRemoteTcpPeer> ConnectAsync(CancellationToken cancellationToken);
    }
}