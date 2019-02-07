using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Events;
using AsyncNet.Tcp.Client.Events;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Tcp.Remote.Events;

namespace AsyncNet.Tcp.Client
{
    /// <summary>
    /// An interface for asynchronous TCP client
    /// </summary>
    public interface IAsyncNetTcpClient
    {
        /// <summary>
        /// Fires when there was a problem with the client
        /// </summary>
        event EventHandler<TcpClientExceptionEventArgs> ClientExceptionOccured;

        /// <summary>
        /// Fires when client started running, but it's not connected yet to the server
        /// </summary>
        event EventHandler<TcpClientStartedEventArgs> ClientStarted;

        /// <summary>
        /// Fires when client stopped running
        /// </summary>
        event EventHandler<TcpClientStoppedEventArgs> ClientStopped;

        /// <summary>
        /// Fires when connection with the server closes
        /// </summary>
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Fires when connection with the server is established
        /// </summary>
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when TCP frame arrived from the server
        /// </summary>
        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when there was a problem while handling communication with the server
        /// </summary>
        event EventHandler<RemoteTcpPeerExceptionEventArgs> RemoteTcpPeerExceptionOccured;

        /// <summary>
        /// Fires when unhandled exception occured - e.g. when event subscriber throws an exception
        /// </summary>
        event EventHandler<ExceptionEventArgs> UnhandledExceptionOccured;

        /// <summary>
        /// Asynchronously starts the client that will run until connection with the server is closed
        /// </summary>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync();

        /// <summary>
        /// Asynchronously starts the client that will run until connection with the server is closed or <paramref name="cancellationToken"/> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}