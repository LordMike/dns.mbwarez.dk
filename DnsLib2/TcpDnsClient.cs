using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsLib2.Common;
using DnsLib2.Enums;

namespace DnsLib2
{
    public class TcpDnsClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _clientStream;
        private ushort _nextId;

        private byte[] _sendBuffer = new byte[4096];
        private byte[] _receiveBuffer = new byte[65536];
        private int _receiveBufferFilled = 0;
        private CancellationTokenSource _cancellationToken;
        private object _lockObj = new object();
        private ConcurrentDictionary<ushort, TaskCompletionSource<DnsMessageView>> _queries;

        public TimeSpan Timeout { get; set; }

        public TcpDnsClient(IPEndPoint server)
        {
            _cancellationToken = new CancellationTokenSource();
            _queries = new ConcurrentDictionary<ushort, TaskCompletionSource<DnsMessageView>>();

            Timeout = TimeSpan.FromSeconds(3);

            _client = new TcpClient();
            _client.Connect(server);

            if (_client == null)
                throw new InvalidOperationException();

            _clientStream = _client.GetStream();
            BeginReceive(_receiveBuffer.Length);
        }

        private void BeginReceive(int toReceive)
        {
            try
            {
                _clientStream.ReadAsync(_receiveBuffer, _receiveBufferFilled, toReceive, _cancellationToken.Token)
                    .ContinueWith(OnReceive, _cancellationToken.Token);
            }
            catch (Exception ex)
            {
                if (_cancellationToken.IsCancellationRequested)
                    // Ignore all errors - we're closing down
                    return;

                if (ex is IOException && ex.InnerException is SocketException)
                {
                    SocketException inner = (SocketException)ex.InnerException;

                    Debug.WriteLine("Socket was " + inner.SocketErrorCode);
                    return;
                }

                throw;
            }
        }

        private void OnReceive(Task<int> result)
        {
            try
            {
                OnReceive(result.Result);
            }
            catch (AggregateException)
            {
                // Shut down this client
                Dispose();
            }
        }

        private void OnReceive(int justRead)
        {
            // Add to received length
            _receiveBufferFilled += justRead;

            if (_receiveBufferFilled < 2)
            {
                // Re-read to get length
                BeginReceive(2 - _receiveBufferFilled);
                return;
            }

            // Read out length
            ushort respLength = (ushort)(_receiveBuffer[0] << 8 | _receiveBuffer[1]);

            // Have we got it?
            if (_receiveBufferFilled < 2 + respLength)
            {
                // Re-read to get data
                BeginReceive((2 + respLength) - _receiveBufferFilled);
                return;
            }

            // Copy out
            byte[] tmpData = new byte[respLength];
            Array.Copy(_receiveBuffer, 2, tmpData, 0, tmpData.Length);

            // Move receive buffer up
            Array.Copy(_receiveBuffer, _receiveBufferFilled, _receiveBuffer, 0, _receiveBuffer.Length - _receiveBufferFilled);
            _receiveBufferFilled = 0;

            // Handle
            try
            {
                DnsMessageView view = new DnsMessageView(tmpData, 0);

                TaskCompletionSource<DnsMessageView> tmp;
                if (!_queries.TryRemove(view.Header.Id, out tmp))
                    return;

                tmp.TrySetResult(view);
            }
            catch (Exception)
            {
                Debug.WriteLine("Something happened :|");
            }

            // Re-read (call self)
            OnReceive(0);
        }

        private void MarkTimeout(ushort key)
        {
            TaskCompletionSource<DnsMessageView> tmp;
            if (!_queries.TryRemove(key, out tmp))
                return;

            tmp.TrySetCanceled();
        }

        private void SendInternal(byte[] data, int length)
        {
            _clientStream.WriteByte((byte)((length >> 8) & 0xFF));
            _clientStream.WriteByte((byte)(length & 0xFF));

            _clientStream.Write(data, 0, length);
        }

        public Task<DnsMessageView> Query(DnsMessageBuilder builder)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource<DnsMessageView> source = new TaskCompletionSource<DnsMessageView>();
                source.TrySetCanceled();

                return source.Task;
            }

            lock (_lockObj)
            {
                ushort id = _nextId++;
                TaskCompletionSource<DnsMessageView> source = new TaskCompletionSource<DnsMessageView>();

                bool wasAdded = _queries.TryAdd(id, source);
                if (!wasAdded)
                    throw new Exception("Huh?");

                builder.SetId(id);
                int length = builder.Encode(_sendBuffer, 0);

                SendInternal(_sendBuffer, length);

                Task.Delay(Timeout).ContinueWith(_ => MarkTimeout(id));

                return source.Task;
            }
        }

        public IEnumerable<Task<DnsMessageView>> Query(DnsMessageBuilder @base, IEnumerable<FQDN> names, QType type = QType.A, QClass @class = QClass.IN)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource<DnsMessageView> source = new TaskCompletionSource<DnsMessageView>();
                source.TrySetCanceled();

                foreach (FQDN name in names)
                    yield return source.Task;

                yield break;
            }

            lock (_lockObj)
            {
                foreach (FQDN name in names)
                {
                    ushort id = _nextId++;
                    TaskCompletionSource<DnsMessageView> source = new TaskCompletionSource<DnsMessageView>();

                    bool wasAdded = _queries.TryAdd(id, source);
                    if (!wasAdded)
                        throw new Exception("Huh?");

                    @base.SetId(id).SetQuestion(name, type, @class);

                    int length = @base.Encode(_sendBuffer, 0);
                    SendInternal(_sendBuffer, length);

                    Task.Delay(Timeout).ContinueWith(_ => MarkTimeout(id));

                    yield return source.Task;
                }

                _clientStream.Flush();
            }
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _client.Close();

            while (_queries.Any())
            {
                ushort[] keys = _queries.Keys.ToArray();
                foreach (ushort key in keys)
                    MarkTimeout(key);
            }
        }
    }
}