using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Extensions;
using AsyncNet.Tcp.Server.Events;
using AsyncNet.Tcp.Remote.Events;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Core.Events;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AsyncNet.Tcp.Server
{
    /// <summary>
    /// An implementation of asynchronous TCP server
    /// </summary>
    public class AsyncNetTcpServer : IAsyncNetTcpServer
    {
        private readonly ConcurrentDictionary<IPEndPoint, RemoteTcpPeer> connectedPeersDictionary = new ConcurrentDictionary<IPEndPoint, RemoteTcpPeer>();

        /// <summary>
        /// Constructs TCP server that runs on particular port and has default configuration
        /// </summary>
        /// <param name="port">A port that TCP server will run on</param>
        public AsyncNetTcpServer(int port) : this(new AsyncNetTcpServerConfig()
            {
                Port = port
            })
        {
        }

        /// <summary>
        /// Constructs TCP server with custom configuration
        /// </summary>
        /// <param name="config">TCP server configuration</param>
        public AsyncNetTcpServer(AsyncNetTcpServerConfig config)
        {
            this.Config = new AsyncNetTcpServerConfig()
            {
                ProtocolFrameDefragmenterFactory = config.ProtocolFrameDefragmenterFactory,
                ConnectionTimeout = config.ConnectionTimeout,
                MaxSendQueuePerPeerSize = config.MaxSendQueuePerPeerSize,
                IPAddress = config.IPAddress,
                Port = config.Port,
                ConfigureTcpListenerCallback = config.ConfigureTcpListenerCallback,
                UseSsl = config.UseSsl,
                X509Certificate = config.X509Certificate,
                RemoteCertificateValidationCallback = config.RemoteCertificateValidationCallback,
                EncryptionPolicy = config.EncryptionPolicy,
                ClientCertificateRequiredCallback = config.ClientCertificateRequiredCallback,
                CheckCertificateRevocationCallback = config.CheckCertificateRevocationCallback,
                EnabledProtocols = config.EnabledProtocols
            };
        }

        /// <summary>
        /// Fires when server started running
        /// </summary>
        public event EventHandler<TcpServerStartedEventArgs> ServerStarted;

        /// <summary>
        /// Fires when server stopped running 
        /// </summary>
        public event EventHandler<TcpServerStoppedEventArgs> ServerStopped;

        /// <summary>
        /// Fires when TCP frame arrived from particular client/peer
        /// </summary>
        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when there was a problem with the server
        /// </summary>
        public event EventHandler<TcpServerExceptionEventArgs> ServerExceptionOccured;

        /// <summary>
        /// Fires when there was an error while handling particular client/peer
        /// </summary>
        public event EventHandler<RemoteTcpPeerExceptionEventArgs> RemoteTcpPeerExceptionOccured;

        /// <summary>
        /// Fires when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        public event EventHandler<ExceptionEventArgs> UnhandledExceptionOccured;

        /// <summary>
        /// Fires when new client/peer connects to the server
        /// </summary>
        public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when connection closes for particular client/peer
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// A list of connected peers / client
        /// </summary>
        public IEnumerable<IRemoteTcpPeer> ConnectedPeers => this.connectedPeersDictionary.Values;

        /// <summary>
        /// Asynchronously starts the server that will run indefinitely
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously starts the server that will run until <paramref name="cancellationToken" /> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            var tcpListener = this.CreateTcpListener();

            this.Config.ConfigureTcpListenerCallback?.Invoke(tcpListener);

            try
            {
                tcpListener.Start();
            }
            catch (Exception ex)
            {
                var e = new TcpServerExceptionEventArgs(ex);

                this.OnServerExceptionOccured(e);

                return;
            }

            try
            {
                await Task.WhenAll(
                    this.ListenAsync(tcpListener, cancellationToken),
                    Task.Run(() => this.OnServerStarted(new TcpServerStartedEventArgs()
                    {
                        ServerAddress = this.Config.IPAddress,
                        ServerPort = this.Config.Port
                    })))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var e = new TcpServerExceptionEventArgs(ex);

                this.OnServerExceptionOccured(e);
            }
            finally
            {
                try
                {
                    tcpListener.Stop();
                }
                catch (Exception ex)
                {
                    var e = new TcpServerExceptionEventArgs(ex);

                    this.OnServerExceptionOccured(e);
                }

                this.OnServerStopped(new TcpServerStoppedEventArgs());
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task BroadcastAsync(byte[] data, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (var peer in peers)
            {
                await peer.SendAsync(data).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task BroadcastAsync(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (var peer in peers)
            {
                await peer.SendAsync(buffer, offset, count).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task BroadcastAsync(byte[] data, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken)
        {
            foreach (var peer in peers)
            {
                await peer.SendAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task BroadcastAsync(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken)
        {
            foreach (var peer in peers)
            {
                await peer.SendAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task AddToBroadcastQueue(byte[] data, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (var peer in peers)
            {
                await peer.AddToSendQueueAsync(data).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task AddToBroadcastQueue(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (var peer in peers)
            {
                await peer.AddToSendQueueAsync(buffer, offset, count).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task AddToBroadcastQueue(byte[] data, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken)
        {
            foreach (var peer in peers)
            {
                await peer.AddToSendQueueAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task AddToBroadcastQueue(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken)
        {
            foreach (var peer in peers)
            {
                await peer.AddToSendQueueAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends the data to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        public virtual void Broadcast(byte[] data, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (var peer in peers)
            {
                peer.Post(data);
            }
        }

        /// <summary>
        /// Sends the data to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        public virtual void Broadcast(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (var peer in peers)
            {
                peer.Post(buffer, offset, count);
            }
        }

        protected virtual AsyncNetTcpServerConfig Config { get; set; }

        protected virtual TcpListener CreateTcpListener()
        {
            return new TcpListener(new IPEndPoint(this.Config.IPAddress, this.Config.Port));
        }

        protected virtual async Task ListenAsync(TcpListener tcpListener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tcpClient = await AcceptTcpClient(tcpListener, token).ConfigureAwait(false);

                    this.HandleNewTcpClientAsync(tcpClient, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        protected virtual Task<TcpClient> AcceptTcpClient(TcpListener tcpListener, CancellationToken token)
        {
            return tcpListener.AcceptTcpClientWithCancellationTokenAsync(token);
        }

        // exploiting "async void" simplifies everything
        protected virtual async void HandleNewTcpClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            try
            {
                await this.HandleNewTcpClientTask(tcpClient, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var se = new TcpServerExceptionEventArgs(ex);

                this.OnServerExceptionOccured(se);
            }
        }

        protected virtual async Task HandleNewTcpClientTask(TcpClient tcpClient, CancellationToken token)
        {
            using (tcpClient)
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var sendQueue = this.CreateSendQueueActionBlock(linkedCts.Token);

                RemoteTcpPeer remoteTcpPeer;
                SslStream sslStream = null;

                try
                {
                    if (this.Config.UseSsl && this.Config.X509Certificate != null)
                    {
                        sslStream = this.CreateSslStream(tcpClient);

                        await this.AuthenticateSslStream(tcpClient, sslStream, linkedCts.Token)
                            .ConfigureAwait(false);

                        remoteTcpPeer = this.CreateRemoteTcpPeer(tcpClient, sslStream, sendQueue, linkedCts);
                    }
                    else
                    {
                        remoteTcpPeer = this.CreateRemoteTcpPeer(tcpClient, sendQueue, linkedCts);
                    }
                }
                catch (AuthenticationException ex)
                {
                    var e = new TcpServerExceptionEventArgs(ex);
                    this.OnServerExceptionOccured(e);

                    sendQueue.Complete();
                    sslStream?.Dispose();

                    return;
                }
                catch (Exception ex)
                {
                    var e = new TcpServerExceptionEventArgs(ex);
                    this.OnServerExceptionOccured(e);

                    sendQueue.Complete();
                    return;
                }

                using (remoteTcpPeer)
                {
                    this.AddRemoteTcpPeerToConnectedList(remoteTcpPeer);

                    var connectionEstablishedEventArgs = new ConnectionEstablishedEventArgs(remoteTcpPeer);
                    this.OnConnectionEstablished(connectionEstablishedEventArgs);

                    await this.HandleRemotePeerAsync(remoteTcpPeer, linkedCts.Token).ConfigureAwait(false);

                    sendQueue.Complete();
                    sslStream?.Dispose();
                }
            }
        }

        protected virtual void AddRemoteTcpPeerToConnectedList(RemoteTcpPeer remoteTcpPeer)
        {
            this.RemoveRemoteTcpPeerFromConnectedList(remoteTcpPeer.IPEndPoint);

            this.connectedPeersDictionary.TryAdd(remoteTcpPeer.IPEndPoint, remoteTcpPeer);
        }

        protected virtual void RemoveRemoteTcpPeerFromConnectedList(IPEndPoint ipEndPoint)
        {
            this.connectedPeersDictionary.TryRemove(ipEndPoint, out var oldTcpPeer);
        }

        protected virtual ActionBlock<RemoteTcpPeerOutgoingMessage> CreateSendQueueActionBlock(CancellationToken token)
        {
            return new ActionBlock<RemoteTcpPeerOutgoingMessage>(
                this.SendToRemotePeerAsync,
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = true,
                    BoundedCapacity = this.Config.MaxSendQueuePerPeerSize,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = token
                });
        }

        protected virtual SslStream CreateSslStream(TcpClient tcpClient)
        {
            return new SslStream(
                tcpClient.GetStream(),
                false,
                this.Config.RemoteCertificateValidationCallback,
                this.SelectDefaultLocalCertificate,
                this.Config.EncryptionPolicy);
        }

        protected virtual X509Certificate SelectDefaultLocalCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            return this.Config.X509Certificate;
        }

        protected virtual Task AuthenticateSslStream(TcpClient tcpClient, SslStream sslStream, CancellationToken token)
        {
            return sslStream.AuthenticateAsServerWithCancellationAsync(
                this.Config.X509Certificate,
                this.Config.ClientCertificateRequiredCallback(tcpClient),
                this.Config.EnabledProtocols,
                this.Config.CheckCertificateRevocationCallback(tcpClient),
                token);
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(TcpClient tcpClient, ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(
                            this.Config.ProtocolFrameDefragmenterFactory,
                            tcpClient,
                            sendQueue,
                            tokenSource,
                            this.OnRemoteTcpPeerExceptionOccured);
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(TcpClient tcpClient, SslStream sslStream, ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(
                            this.Config.ProtocolFrameDefragmenterFactory,
                            tcpClient,
                            sslStream,
                            sendQueue,
                            tokenSource,
                            this.OnRemoteTcpPeerExceptionOccured);
        }

        protected virtual async Task HandleRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            try
            {
                await this.ReceiveFromRemotePeerAsync(remoteTcpPeer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.Timeout;
                }
                else
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
                }

                remoteTcpPeer.ConnectionCloseException = ex;
            }
            catch (Exception ex)
            {
                remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.ExceptionOccured;
                remoteTcpPeer.ConnectionCloseException = ex;

                var ue = new ExceptionEventArgs(ex);

                this.OnUnhandledException(ue);
            }

            this.RemoveRemoteTcpPeerFromConnectedList(remoteTcpPeer.IPEndPoint);

            var connectionClosedEventArgs = new ConnectionClosedEventArgs(remoteTcpPeer)
            {
                ConnectionCloseException = remoteTcpPeer.ConnectionCloseException,
                ConnectionCloseReason = remoteTcpPeer.ConnectionCloseReason
            };

            this.OnConnectionClosed(remoteTcpPeer, connectionClosedEventArgs);
        }

        protected virtual async Task ReceiveFromRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            Defragmentation.ReadFrameResult readFrameResult = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                using (var timeoutCts = this.Config.ConnectionTimeout == TimeSpan.Zero ? new CancellationTokenSource() : new CancellationTokenSource(this.Config.ConnectionTimeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                {
                    readFrameResult = await remoteTcpPeer.ProtocolFrameDefragmenter
                        .ReadFrameAsync(remoteTcpPeer, readFrameResult?.LeftOvers, linkedCts.Token)
                        .ConfigureAwait(false);
                }

                if (readFrameResult.ReadFrameStatus == Defragmentation.ReadFrameStatus.StreamClosed)
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.RemoteShutdown;
                    return;
                }
                else if (readFrameResult.ReadFrameStatus == Defragmentation.ReadFrameStatus.FrameDropped)
                {
                    readFrameResult = null;

                    continue;
                }

                var frameArrivedEventArgs = new TcpFrameArrivedEventArgs(remoteTcpPeer, readFrameResult.FrameData);

                this.OnFrameArrived(remoteTcpPeer, frameArrivedEventArgs);
            }

            remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
        }

        protected virtual async Task SendToRemotePeerAsync(RemoteTcpPeerOutgoingMessage outgoingMessage)
        {
            try
            {
                await outgoingMessage.RemoteTcpPeer.TcpStream.WriteWithRealCancellationAsync(
                    outgoingMessage.Buffer.Memory,
                    outgoingMessage.Buffer.Offset,
                    outgoingMessage.Buffer.Count,
                    outgoingMessage.CancellationToken).ConfigureAwait(false);

                await outgoingMessage.RemoteTcpPeer.TcpStream.FlushAsync(outgoingMessage.CancellationToken).ConfigureAwait(false);

                outgoingMessage.SendTaskCompletionSource.TrySetResult(true);
            }
            catch (OperationCanceledException ex)
            {
                outgoingMessage.SendTaskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                var remoteTcpPeerErrorEventArgs = new RemoteTcpPeerExceptionEventArgs(outgoingMessage.RemoteTcpPeer, ex);

                this.OnRemoteTcpPeerExceptionOccured(remoteTcpPeerErrorEventArgs);
            }
        }

        protected virtual void OnConnectionEstablished(ConnectionEstablishedEventArgs e)
        {
            try
            {
                this.ConnectionEstablished?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);

                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnFrameArrived(RemoteTcpPeer remoteTcpPeer, TcpFrameArrivedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnFrameArrived(e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }

            try
            {
                this.FrameArrived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnConnectionClosed(RemoteTcpPeer remoteTcpPeer, ConnectionClosedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnConnectionClosed(e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }

            try
            {
                this.ConnectionClosed?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnServerStarted(TcpServerStartedEventArgs e)
        {
            try
            {
                this.ServerStarted?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnServerStopped(TcpServerStoppedEventArgs e)
        {
            try
            {
                this.ServerStopped?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnServerExceptionOccured(TcpServerExceptionEventArgs e)
        {
            try
            {
                this.ServerExceptionOccured?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnRemoteTcpPeerExceptionOccured(RemoteTcpPeerExceptionEventArgs e)
        {
            try
            {
                this.RemoteTcpPeerExceptionOccured?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var ue = new ExceptionEventArgs(ex);
                this.OnUnhandledException(ue);
            }
        }

        protected virtual void OnUnhandledException(ExceptionEventArgs e)
        {
            this.UnhandledExceptionOccured?.Invoke(this, e);
        }
    }
}
