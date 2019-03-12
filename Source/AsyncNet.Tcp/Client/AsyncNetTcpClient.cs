using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Core.Events;
using AsyncNet.Core.Extensions;
using AsyncNet.Tcp.Client.Events;
using AsyncNet.Tcp.Connection;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Tcp.Extensions;
using AsyncNet.Tcp.Remote;
using AsyncNet.Tcp.Remote.Events;

namespace AsyncNet.Tcp.Client
{
    /// <summary>
    /// An implementation of asynchronous TCP client
    /// </summary>
    public class AsyncNetTcpClient : IAsyncNetTcpClient
    {
        /// <summary>
        /// Constructs TCP client that connects to the particular server and has default configuration
        /// </summary>
        /// <param name="targetHostname">Server hostname</param>
        /// <param name="targetPort">Server port</param>
        public AsyncNetTcpClient(string targetHostname, int targetPort) : this (new AsyncNetTcpClientConfig()
        {
            TargetHostname = targetHostname,
            TargetPort = targetPort
        })
        {
        }

        /// <summary>
        /// Constructs TCP client with custom configuration
        /// </summary>
        /// <param name="config">TCP client configuration</param>
        public AsyncNetTcpClient(AsyncNetTcpClientConfig config)
        {
            this.Config = new AsyncNetTcpClientConfig()
            {
                ProtocolFrameDefragmenterFactory = config.ProtocolFrameDefragmenterFactory,
                TargetHostname = config.TargetHostname,
                TargetPort = config.TargetPort,
                ConnectionTimeout = config.ConnectionTimeout,
                MaxSendQueueSize = config.MaxSendQueueSize,
                ConfigureTcpClientCallback = config.ConfigureTcpClientCallback,
                FilterResolvedIpAddressListForConnectionCallback = config.FilterResolvedIpAddressListForConnectionCallback,
                UseSsl = config.UseSsl,
                X509ClientCertificates = config.X509ClientCertificates,
                RemoteCertificateValidationCallback = config.RemoteCertificateValidationCallback,
                LocalCertificateSelectionCallback = config.LocalCertificateSelectionCallback,
                EncryptionPolicy = config.EncryptionPolicy,
                CheckCertificateRevocation = config.CheckCertificateRevocation,
                EnabledProtocols = config.EnabledProtocols
            };
        }

        /// <summary>
        /// Fires when client started running, but it's not connected yet to the server
        /// </summary>
        public event EventHandler<TcpClientStartedEventArgs> ClientStarted;

        /// <summary>
        /// Fires when client stopped running
        /// </summary>
        public event EventHandler<TcpClientStoppedEventArgs> ClientStopped;

        /// <summary>
        /// Fires when there was a problem with the client
        /// </summary>
        public event EventHandler<TcpClientExceptionEventArgs> ClientExceptionOccured;

        /// <summary>
        /// Fires when there was a problem while handling communication with the server
        /// </summary>
        public event EventHandler<RemoteTcpPeerExceptionEventArgs> RemoteTcpPeerExceptionOccured;

        /// <summary>
        /// Fires when unhandled exception occured - e.g. when event subscriber throws an exception
        /// </summary>
        public event EventHandler<ExceptionEventArgs> UnhandledExceptionOccured;

        /// <summary>
        /// Fires when connection with the server is established
        /// </summary>
        public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when TCP frame arrived from the server
        /// </summary>
        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when connection with the server closes
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Asynchronously starts the client that run until connection with the server is closed
        /// </summary>
        /// <returns><see cref="Task" /></returns>
        public virtual Task StartAsync()
        {
            return this.StartAsync(CancellationToken.None);
        }

        /// <summary>
        /// Asynchronously starts the client that run until connection with the server is closed or <paramref name="cancellationToken" /> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="Task" /></returns>
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            IPAddress[] addresses;

            var tcpClient = this.CreateTcpClient();

            this.Config.ConfigureTcpClientCallback?.Invoke(tcpClient);

            this.OnClientStarted(new TcpClientStartedEventArgs()
            {
                TargetHostname = this.Config.TargetHostname,
                TargetPort = this.Config.TargetPort
            });

            try
            {
                addresses = await this.GetHostAddresses(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                this.OnClientStopped(new TcpClientStoppedEventArgs()
                {
                    ClientStoppedReason = ClientStoppedReason.InitiatingConnectionTimeout,
                    Exception = ex
                });

#if (NET45 == false)
                tcpClient.Dispose();
#endif
                return;
            }
            catch (Exception ex)
            {
                this.OnClientExceptionOccured(new TcpClientExceptionEventArgs(ex));

                this.OnClientStopped(new TcpClientStoppedEventArgs()
                {
                    ClientStoppedReason = ClientStoppedReason.RuntimeException,
                    Exception = ex
                });

#if (NET45 == false)
                tcpClient.Dispose();
#endif
                return;
            }

            try
            {
                await this.ConnectAsync(tcpClient, addresses, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
#if (NET45 == false)
                tcpClient.Dispose();
#endif
                this.OnClientStopped(new TcpClientStoppedEventArgs()
                {
                    ClientStoppedReason = ClientStoppedReason.InitiatingConnectionTimeout,
                    Exception = ex
                });

                return;
            }
            catch (Exception ex)
            {
                this.OnClientExceptionOccured(new TcpClientExceptionEventArgs(ex));

#if (NET45 == false)
                tcpClient.Dispose();
#endif

                this.OnClientStopped(new TcpClientStoppedEventArgs()
                {
                    ClientStoppedReason = ClientStoppedReason.RuntimeException,
                    Exception = ex
                });

                return;
            }

            try
            {
                await this.HandleTcpClientAsync(tcpClient, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var ce = new TcpClientExceptionEventArgs(ex);

                this.OnClientExceptionOccured(ce);
            }
        }

        protected virtual AsyncNetTcpClientConfig Config { get; set; }

        protected virtual TcpClient CreateTcpClient()
        {
            return new TcpClient();
        }

        protected virtual Task<IPAddress[]> GetHostAddresses(CancellationToken cancellationToken)
        {
            return DnsExtensions.GetHostAddressesWithCancellationTokenAsync(this.Config.TargetHostname, cancellationToken);
        }

        protected virtual IEnumerable<IPAddress> DefaultIpAddressFilter(IPAddress[] addresses)
        {
            return addresses;
        }

        protected virtual async Task ConnectAsync(TcpClient tcpClient, IPAddress[] addresses, CancellationToken cancellationToken)
        {
            IEnumerable<IPAddress> filteredAddressList;

            if (addresses != null || addresses.Length > 0)
            {
                if (this.Config.FilterResolvedIpAddressListForConnectionCallback != null)
                {
                    filteredAddressList = this.Config.FilterResolvedIpAddressListForConnectionCallback(addresses);
                }
                else
                {
                    filteredAddressList = this.DefaultIpAddressFilter(addresses);
                }

                await tcpClient.ConnectWithCancellationTokenAsync(filteredAddressList.ToArray(), this.Config.TargetPort, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await tcpClient.ConnectWithCancellationTokenAsync(this.Config.TargetHostname, this.Config.TargetPort, cancellationToken).ConfigureAwait(false);
            }
        }

        protected virtual ActionBlock<RemoteTcpPeerOutgoingMessage> CreateSendQueueActionBlock(CancellationToken token)
        {
            return new ActionBlock<RemoteTcpPeerOutgoingMessage>(
                this.SendToRemotePeerAsync,
                new ExecutionDataflowBlockOptions()
                {
                    EnsureOrdered = true,
                    BoundedCapacity = this.Config.MaxSendQueueSize,
                    MaxDegreeOfParallelism = 1,
                    CancellationToken = token
                });
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

#if NET45
            catch (OperationCanceledException)
            {
                outgoingMessage.SendTaskCompletionSource.TrySetCanceled();
            }
#else
            catch (OperationCanceledException ex)
            {
                outgoingMessage.SendTaskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
#endif
            catch (Exception ex)
            {
                var remoteTcpPeerExceptionEventArgs = new RemoteTcpPeerExceptionEventArgs(outgoingMessage.RemoteTcpPeer, ex);

                this.OnRemoteTcpPeerExceptionOccured(remoteTcpPeerExceptionEventArgs);
            }
        }

        protected virtual async Task HandleTcpClientAsync(TcpClient tcpClient, CancellationToken token)
        {
            using (tcpClient)
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                var sendQueue = this.CreateSendQueueActionBlock(linkedCts.Token);

                RemoteTcpPeer remoteTcpPeer;
                SslStream sslStream = null;

                try
                {
                    if (this.Config.UseSsl)
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
                    var clientExceptionEventArgs = new TcpClientExceptionEventArgs(ex);
                    this.OnClientExceptionOccured(clientExceptionEventArgs);

                    sendQueue.Complete();
                    sslStream?.Dispose();

                    this.OnClientStopped(new TcpClientStoppedEventArgs()
                    {
                        ClientStoppedReason = ClientStoppedReason.RuntimeException,
                        Exception = ex
                    });

                    return;
                }
                catch (Exception ex)
                {
                    sendQueue.Complete();

                    this.OnClientStopped(new TcpClientStoppedEventArgs()
                    {
                        ClientStoppedReason = ClientStoppedReason.RuntimeException,
                        Exception = ex
                    });

                    return;
                }

                using (remoteTcpPeer)
                {
                    var connectionEstablishedEventArgs = new ConnectionEstablishedEventArgs(remoteTcpPeer);
                    this.OnConnectionEstablished(connectionEstablishedEventArgs);

                    await this.HandleRemotePeerAsync(remoteTcpPeer, linkedCts.Token).ConfigureAwait(false);

                    sendQueue.Complete();
                    sslStream?.Dispose();

                    this.OnClientStopped(new TcpClientStoppedEventArgs()
                    {
                        ClientStoppedReason = ClientStoppedReason.Disconnected,
                        ConnectionCloseReason = remoteTcpPeer.ConnectionCloseReason,
                        Exception = remoteTcpPeer.ConnectionCloseException
                    });
                }
            }
        }

        protected virtual SslStream CreateSslStream(TcpClient tcpClient)
        {
            LocalCertificateSelectionCallback certificateSelectionCallback;

            certificateSelectionCallback = this.Config.LocalCertificateSelectionCallback;

            if (certificateSelectionCallback == null)
            {
                certificateSelectionCallback = this.SelectDefaultLocalCertificate;
            }

            return new SslStream(
                tcpClient.GetStream(),
                false,
                this.Config.RemoteCertificateValidationCallback,
                certificateSelectionCallback,
                this.Config.EncryptionPolicy);
        }

        protected virtual X509Certificate SelectDefaultLocalCertificate(
            object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            if (acceptableIssuers != null
                && acceptableIssuers.Length > 0
                && localCertificates != null
                && localCertificates.Count > 0)
            {
                foreach (X509Certificate certificate in localCertificates)
                {
                    string issuer = certificate.Issuer;
                    if (Array.IndexOf(acceptableIssuers, issuer) != -1)
                        return certificate;
                }
            }
            if (localCertificates != null
                && localCertificates.Count > 0)
            {
                return localCertificates[0];
            }

            return null;
        }

        protected virtual Task AuthenticateSslStream(TcpClient tcpClient, SslStream sslStream, CancellationToken token)
        {
            return sslStream.AuthenticateAsClientWithCancellationAsync(
                this.Config.TargetHostname,
                new X509CertificateCollection(this.Config.X509ClientCertificates?.ToArray() ?? new X509Certificate[] { }),
                this.Config.EnabledProtocols,
                this.Config.CheckCertificateRevocation,
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

                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
            }

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

        protected virtual void OnClientStarted(TcpClientStartedEventArgs e)
        {
            try
            {
                this.ClientStarted?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
            }
        }

        protected virtual void OnClientStopped(TcpClientStoppedEventArgs e)
        {
            try
            {
                this.ClientStopped?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
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
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
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
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
            }

            try
            {
                this.FrameArrived?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
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
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
            }

            try
            {
                this.ConnectionClosed?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
            }
        }

        protected virtual void OnClientExceptionOccured(TcpClientExceptionEventArgs e)
        {
            try
            {
                this.ClientExceptionOccured?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
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
                var unhandledExceptionEventArgs = new ExceptionEventArgs(ex);

                this.OnUnhandledException(unhandledExceptionEventArgs);
            }
        }

        protected virtual void OnUnhandledException(ExceptionEventArgs e)
        {
            this.UnhandledExceptionOccured?.Invoke(this, e);
        }
    }
}
