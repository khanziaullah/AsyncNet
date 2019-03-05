using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Core.Events;
using AsyncNet.Tcp.Connection.Events;
using AsyncNet.Tcp.Remote;
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
        /// A list of connected peers / client
        /// </summary>
        IEnumerable<IRemoteTcpPeer> ConnectedPeers { get; }

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

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task"/></returns>
        Task BroadcastAsync(byte[] data, IEnumerable<IRemoteTcpPeer> peers);

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task"/></returns>
        Task BroadcastAsync(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers);

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task"/></returns>
        Task BroadcastAsync(byte[] data, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task"/></returns>
        Task BroadcastAsync(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken);

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task"/></returns>
        Task AddToBroadcastQueue(byte[] data, IEnumerable<IRemoteTcpPeer> peers);

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="Task"/></returns>
        Task AddToBroadcastQueue(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers);

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task"/></returns>
        Task AddToBroadcastQueue(byte[] data, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken);

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task"/></returns>
        Task AddToBroadcastQueue(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the data to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        void Broadcast(byte[] data, IEnumerable<IRemoteTcpPeer> peers);

        /// <summary>
        /// Sends the data to all connected peers / clients from <paramref name="peers"/> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        void Broadcast(byte[] buffer, int offset, int count, IEnumerable<IRemoteTcpPeer> peers);
    }
}