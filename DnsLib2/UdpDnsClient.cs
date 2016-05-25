using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsLib2
{
    public class UdpDnsClient : IDnsClient, IDisposable
    {
        private UdpClient _client;
        private ushort _nextId;

        private byte[] _sendBuffer = new byte[4096];
        private CancellationTokenSource _cancellationToken;
        private object _lockObj = new object();
        private ConcurrentDictionary<ushort, TaskCompletionSource<DnsMessageView>> _queries;

        public TimeSpan Timeout { get; set; }

        public UdpDnsClient(AddressFamily addressFamily)
        {
            _cancellationToken = new CancellationTokenSource();
            _queries = new ConcurrentDictionary<ushort, TaskCompletionSource<DnsMessageView>>();

            Timeout = TimeSpan.FromSeconds(3);

            _client = null;
            for (int i = 11000; i < 12000; i++)
            {
                try
                {
                    _client = new UdpClient(i, addressFamily);
                    break;
                }
                catch (Exception)
                {

                }
            }

            if (_client == null)
                throw new InvalidOperationException();

            _client.ReceiveAsync().ContinueWith(OnReceive, _cancellationToken.Token);
        }

        private void OnReceive(Task<UdpReceiveResult> task)
        {
            // Begin next
            _client.ReceiveAsync().ContinueWith(OnReceive, _cancellationToken.Token);

            // Handle
            try
            {
                DnsMessageView view = new DnsMessageView(task.Result.Buffer, 0);

                TaskCompletionSource<DnsMessageView> tmp;
                if (!_queries.TryRemove(view.Header.Id, out tmp))
                    return;

                tmp.TrySetResult(view);
            }
            catch (Exception)
            {
                Debug.WriteLine("Something happened :|");
            }
        }

        private void MarkTimeout(ushort key)
        {
            TaskCompletionSource<DnsMessageView> tmp;
            if (!_queries.TryRemove(key, out tmp))
                return;

            tmp.TrySetCanceled();
        }

        public Task<DnsMessageView> Query(IPEndPoint server, DnsMessageBuilder builder)
        {
            lock (_lockObj)
            {
                ushort id = _nextId++;
                TaskCompletionSource<DnsMessageView> source = new TaskCompletionSource<DnsMessageView>();

                bool wasAdded = _queries.TryAdd(id, source);
                if (!wasAdded)
                    throw new Exception("Huh?");

                builder.SetId(id);
                int length = builder.Encode(_sendBuffer, 0);
                _client.Send(_sendBuffer, length, server);

                Task.Delay(Timeout).ContinueWith(_ => MarkTimeout(id));

                return source.Task;
            }
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _client.Close();
        }
    }
}