using System;
using System.Collections.Concurrent;

namespace Common.ClientSpecificWorkers
{
    public class ClientRegister<TRequest, TResponse>
    {
        private ConcurrentDictionary<Guid, Consumer<TRequest, TResponse>> _clients = new ConcurrentDictionary<Guid, Consumer<TRequest, TResponse>>();

        private Func<TRequest, TResponse> _func;

        public ClientRegister(Func<TRequest, TResponse> func)
        {
            _func = func;
        }

        public bool AllDone { get { return _clients.Count == 0;  } }

        public void ClientOnline(Guid id)
        {
            if (!_clients.ContainsKey(id))
            {
                var consumer = new Consumer<TRequest, TResponse>(_func, id);

                if (_clients.TryAdd(id, consumer))
                {
                    Console.WriteLine("[R] Starting worker for: {0}", id.ToPrintable());
                    consumer.Start();
                }
            }
        }

        public void ClientOffline(Guid id)
        {
            if (_clients.ContainsKey(id))
            {
                Consumer<TRequest, TResponse> consumer;

                if (_clients.TryRemove(id, out consumer))
                {
                    Console.WriteLine("[R] Stopping worker for: {0}", id.ToPrintable());
                    consumer.Stop();
                }
            }
        }
    }
}