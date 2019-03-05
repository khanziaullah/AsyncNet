using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Tcp.Remote;

namespace AsyncNet.Tcp.Client
{
    public class AwaitaibleAsyncNetTcpClient : IAwaitaibleAsyncNetTcpClient
    {
        private readonly int frameBufferBoundedCapacity;
        private readonly CancellationTokenSource cts;

        private TaskCompletionSource<IAwaitaibleRemoteTcpPeer> tcsConnection;

        public AwaitaibleAsyncNetTcpClient(AsyncNetTcpClient client, int frameBufferBoundedCapacity = -1)
        {
            this.Client = client;
            this.Client.ConnectionEstablished += ConnectionEstablishedCallback;
            this.Client.ClientStopped += ClientStoppedCallback;
            this.frameBufferBoundedCapacity = frameBufferBoundedCapacity;
            this.cts = new CancellationTokenSource();
        }

        public AsyncNetTcpClient Client { get; }

        public void Dispose()
        {
            this.Client.ClientStopped -= ClientStoppedCallback;
            this.Client.ConnectionEstablished -= ConnectionEstablishedCallback;
            this.cts.Cancel();
            this.cts.Dispose();
        }

        public virtual Task<IAwaitaibleRemoteTcpPeer> ConnectAsync() => this.ConnectAsync(CancellationToken.None);

        public virtual async Task<IAwaitaibleRemoteTcpPeer> ConnectAsync(CancellationToken cancellationToken)
        {
            this.tcsConnection = new TaskCompletionSource<IAwaitaibleRemoteTcpPeer>();

            _ = this.Client.StartAsync(this.cts.Token);

            using (var ctr = cancellationToken.Register(() => this.tcsConnection.TrySetCanceled(cancellationToken), false))
            {
                return await this.tcsConnection.Task.ConfigureAwait(false);
            }
        }

        protected virtual void ConnectionEstablishedCallback(object sender, Connection.Events.ConnectionEstablishedEventArgs e)
        {
            this.tcsConnection?.TrySetResult(new AwaitaibleRemoteTcpPeer(e.RemoteTcpPeer, this.frameBufferBoundedCapacity));
        }

        protected virtual void ClientStoppedCallback(object sender, Events.TcpClientStoppedEventArgs e)
        {
            switch (e.ClientStoppedReason)
            {
                case ClientStoppedReason.InitiatingConnectionTimeout:
                    this.tcsConnection?.TrySetCanceled();
                    break;
                case ClientStoppedReason.InitiatingConnectionFailure:
                    this.tcsConnection?.TrySetException(e.Exception);
                    break;
                case ClientStoppedReason.Disconnected:
                    break;
                case ClientStoppedReason.RuntimeException:
                    this.tcsConnection?.TrySetException(e.Exception);
                    break;
            }
        }
    }
}
