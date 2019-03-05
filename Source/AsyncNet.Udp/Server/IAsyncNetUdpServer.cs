using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AsyncNet.Udp.Error.Events;
using AsyncNet.Udp.Remote.Events;
using AsyncNet.Udp.Server.Events;

namespace AsyncNet.Udp.Server
{
    /// <summary>
    /// An interface for asynchronous UDP server
    /// </summary>
    public interface IAsyncNetUdpServer
    {
        /// <summary>
        /// Underlying <see cref="System.Net.Sockets.UdpClient"/>
        /// </summary>
        UdpClient UdpClient { get; }

        /// <summary>
        /// Fires when there was a problemw with the server
        /// </summary>
        event EventHandler<UdpServerExceptionEventArgs> ServerExceptionOccured;

        /// <summary>
        /// Fires when server started running
        /// </summary>
        event EventHandler<UdpServerStartedEventArgs> ServerStarted;

        /// <summary>
        /// Fires when server stopped running
        /// </summary>
        event EventHandler<UdpServerStoppedEventArgs> ServerStopped;

        /// <summary>
        /// Fires when packet arrived from particular client/peer
        /// </summary>
        event EventHandler<UdpPacketArrivedEventArgs> UdpPacketArrived;

        /// <summary>
        /// Fires when there was a problem while sending packet to the target client/peer
        /// </summary>
        event EventHandler<UdpSendErrorEventArgs> UdpSendErrorOccured;

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or server is stopped</returns>
        bool Post(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint);

        /// <summary>
        /// Adds data to the send queue. It will fail if send queue buffer is full returning false
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - send queue buffer is full or server is stopped</returns>
        bool Post(byte[] data, IPEndPoint remoteEndPoint);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] data, IPEndPoint remoteEndPoint);

        /// <summary>
        /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - added to the send queue. False - server is stopped</returns>
        Task<bool> AddToSendQueueAsync(byte[] data, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - data was sent. False - server is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer"/></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - data was sent. False - server is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] buffer, int offset, int count, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <returns>True - data was sent. False - server is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] data, IPEndPoint remoteEndPoint);

        /// <summary>
        /// Sends data asynchronously
        /// </summary>
        /// <param name="data">Data to send</param>
        /// <param name="remoteEndPoint">Client/peer endpoint</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns>True - data was sent. False - server is stopped or underlying send buffer is full</returns>
        Task<bool> SendAsync(byte[] data, IPEndPoint remoteEndPoint, CancellationToken cancellationToken);

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