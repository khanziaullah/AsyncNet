using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AsyncNet.Tcp.Remote.Events;

namespace AsyncNet.Tcp.Remote
{
    public class AwaitaibleRemoteTcpPeer : IAwaitaibleRemoteTcpPeer
    {
        private readonly BufferBlock<byte[]> frameBuffer;

        public AwaitaibleRemoteTcpPeer(IRemoteTcpPeer remoteTcpPeer, int frameBufferBoundedCapacity = -1)
        {
            this.RemoteTcpPeer = remoteTcpPeer;
            this.RemoteTcpPeer.FrameArrived += this.FrameArrivedCallback;
            this.RemoteTcpPeer.ConnectionClosed += this.ConnectionClosedCallback;
            this.frameBuffer = new BufferBlock<byte[]>(new DataflowBlockOptions()
            {
                BoundedCapacity = frameBufferBoundedCapacity
            });
        }

        /// <summary>
        /// Fires when frame buffer is full
        /// </summary>
        public event EventHandler<AddingTcpFrameToFrameBufferFailedEventArgs> AddingTcpFrameToFrameBufferFailed;

        public void Dispose()
        {
            this.RemoteTcpPeer.FrameArrived -= this.FrameArrivedCallback;
            this.RemoteTcpPeer.ConnectionClosed -= this.ConnectionClosedCallback;
            this.frameBuffer.Complete();
        }

        /// <summary>
        /// Underlying remote peer. Use this for sending data
        /// </summary>
        public virtual IRemoteTcpPeer RemoteTcpPeer { get; }

        /// <summary>
        /// Reads tcp frame in an asynchronous manner
        /// </summary>
        /// <returns><see cref="Task{TResult}" /> which returns tcp frame bytes</returns>
        public virtual Task<byte[]> ReadFrameAsync() => this.ReadFrameAsync(CancellationToken.None);

        /// <summary>
        /// Reads tcp frame in an asynchronous manner
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task{TResult}" /> which returns tcp frame bytes</returns>
        public virtual async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await this.frameBuffer.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                return new byte[0];
            }
        }

        /// <summary> 
        /// Reads all bytes from tcp stream until peer disconnects in an asynchronous manner
        /// </summary>
        /// <returns><see cref="Task{TResult}" /> which returns byte array</returns>
        public virtual Task<byte[]> ReadAllBytesAsync() => this.ReadAllBytesAsync(CancellationToken.None);

        /// <summary>
        /// Reads all bytes from tcp stream until peer disconnects in an asynchronous manner
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task{TResult}" /> which returns byte array</returns>
        public virtual async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream())
            {
                byte[] bytes;

                while ((bytes = await this.ReadFrameAsync(cancellationToken).ConfigureAwait(false)).Length > 0)
                {
                    await ms.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Reads all frames until peer disconnects in an asynchronous manner
        /// </summary>
        /// <returns><see cref="Task{TResult}" /> which returns list of frames</returns>
        public virtual Task<IEnumerable<byte[]>> ReadAllFramesAsync() => this.ReadAllFramesAsync(CancellationToken.None);

        /// <summary>
        /// Reads all frames until peer disconnects in an asynchronous manner
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="Task{TResult}" /> which returns list of frames</returns>
        public virtual async Task<IEnumerable<byte[]>> ReadAllFramesAsync(CancellationToken cancellationToken)
        {
            byte[] bytes;
            var list = new List<byte[]>();

            while ((bytes = await this.ReadFrameAsync(cancellationToken).ConfigureAwait(false)).Length > 0)
            {
                list.Add(bytes);
            }

            return list;
        }

        protected virtual void FrameArrivedCallback(object sender, Events.TcpFrameArrivedEventArgs e)
        {
            if (!this.frameBuffer.Post(e.FrameData))
            {
                if (!this.frameBuffer.Completion.IsCompleted)
                {
                    this.OnAddingTcpFrameToFrameBufferFailed(new AddingTcpFrameToFrameBufferFailedEventArgs(this, e.FrameData));
                }
            }
        }

        protected virtual void ConnectionClosedCallback(object sender, Connection.Events.ConnectionClosedEventArgs e)
        {
            this.frameBuffer.Complete();
        }

        protected virtual void OnAddingTcpFrameToFrameBufferFailed(AddingTcpFrameToFrameBufferFailedEventArgs e)
        {
            this.AddingTcpFrameToFrameBufferFailed?.Invoke(this, e);
        }
    }
}
