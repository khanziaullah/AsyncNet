using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Events;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Tcp.Remote.Events;
using AsyncNet.Tcp.Server.Events;

namespace AsyncNet.Tcp.Server
{
    /// <summary>
    /// An interface for asynchronous TCP server
    /// </summary>
    public interface IAsyncNetTcpServer
    {
        /// <summary>
        /// Fires when connection closes for particular client/peer
        /// </summary>
        event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>
        /// Fires when new client/peer connects to the server
        /// </summary>
        event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when TCP frame arrived from particular client/peer
        /// </summary>
        event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>
        /// Fires when there was an exception while handling particular client/peer
        /// </summary>
        event EventHandler<RemoteTcpPeerExceptionEventArgs> RemoteTcpPeerExceptionOccured;

        /// <summary>
        /// Fires when there was a problem with the server
        /// </summary>
        event EventHandler<TcpServerExceptionEventArgs> ServerExceptionOccured;

        /// <summary>
        /// Fires when server started running
        /// </summary>
        event EventHandler<TcpServerStartedEventArgs> ServerStarted;

        /// <summary>
        /// Fires when server stopped running 
        /// </summary>
        event EventHandler<TcpServerStoppedEventArgs> ServerStopped;

        /// <summary>
        /// Fires when unhandled exception occured - e.g. when event subscriber throws an exception
        /// </summary>
        event EventHandler<ExceptionEventArgs> UnhandledExceptionOccured;

        /// <summary>
        /// Asynchronously starts the server that will run indefinitely
        /// </summary>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync();

        /// <summary>
        /// Asynchronously starts the server that will run until <paramref name="cancellationToken"/> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="System.Threading.Tasks.Task"/></returns>
        Task StartAsync(CancellationToken cancellationToken);
    }
}