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

namespace AsyncNet.Tcp.Server
{
    /// <summary>
    /// An implementation of asynchronous TCP server
    /// </summary>
    public class AsyncNetTcpServer : IAsyncNetTcpServer
    {
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
                    Task.Run(() => this.OnServerStarted(new TcpServerStartedEventArgs(this.Config.IPAddress, this.Config.Port))))
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
                var ue = new ExceptionEventArgs(ex);

                this.OnUnhandledException(ue);
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
                catch (Exception)
                {
                    sendQueue.Complete();
                    return;
                }

                using (remoteTcpPeer)
                {
                    var connectionEstablishedEventArgs = new ConnectionEstablishedEventArgs(remoteTcpPeer);
                    this.OnConnectionEstablished(connectionEstablishedEventArgs);

                    try
                    {
                        await this.HandleRemotePeerAsync(remoteTcpPeer, linkedCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var ue = new ExceptionEventArgs(ex);

                        this.OnUnhandledException(ue);
                    }
                    finally
                    {
                        sendQueue.Complete();
                        sslStream?.Dispose();
                    }
                }
            }
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
                            tokenSource);
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(TcpClient tcpClient, SslStream sslStream, ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue, CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(
                            this.Config.ProtocolFrameDefragmenterFactory,
                            tcpClient,
                            sslStream,
                            sendQueue,
                            tokenSource);
        }

        protected virtual async Task HandleRemotePeerAsync(RemoteTcpPeer remoteTcpPeer, CancellationToken cancellationToken)
        {
            try
            {
                await this.ReceiveFromRemotePeerAsync(remoteTcpPeer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.Timeout;
                    return;
                }
                else
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
                    return;
                }
            }
            catch (Exception ex)
            {
                remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.ExceptionOccured;

                var ue = new ExceptionEventArgs(ex);

                this.OnUnhandledException(ue);
            }

            var connectionClosedEventArgs = new ConnectionClosedEventArgs(remoteTcpPeer, remoteTcpPeer.ConnectionCloseReason);
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
                var unhandledErrorEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledErrorEventArgs);
            }
        }

        protected virtual void OnFrameArrived(RemoteTcpPeer remoteTcpPeer, TcpFrameArrivedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnFrameArrived(e);

                this.FrameArrived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledErrorEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledErrorEventArgs);
            }
        }

        protected virtual void OnConnectionClosed(RemoteTcpPeer remoteTcpPeer, ConnectionClosedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnConnectionClosed(e);
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
            this.ServerStarted?.Invoke(this, e);
        }

        protected virtual void OnServerStopped(TcpServerStoppedEventArgs e)
        {
            this.ServerStopped?.Invoke(this, e);
        }

        protected virtual void OnServerExceptionOccured(TcpServerExceptionEventArgs e)
        {
            this.ServerExceptionOccured?.Invoke(this, e);
        }

        protected virtual void OnRemoteTcpPeerExceptionOccured(RemoteTcpPeerExceptionEventArgs e)
        {
            this.RemoteTcpPeerExceptionOccured?.Invoke(this, e);
        }

        protected virtual void OnUnhandledException(ExceptionEventArgs e)
        {
            this.UnhandledExceptionOccured?.Invoke(this, e);
        }
    }
}
