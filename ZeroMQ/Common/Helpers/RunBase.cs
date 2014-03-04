using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public abstract class RunBase : IDisposable
    {
        private bool _disposed;

        private Task _task;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public RunBase()
        {
        }

        ~RunBase()
        {
            Dispose(false);
        }

        protected CancellationToken Token { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            StartMethod();

            _task = Task.Factory.StartNew((t) => Run(t),
                _tokenSource.Token,
                TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            StopMethod();

            _tokenSource.Cancel();

            DisposeMethod();
        }

        protected abstract void StartMethod();

        protected abstract void StopMethod();

        protected abstract void ExecutionMethod();

        protected abstract void DisposeMethod();

        private void Run(object t)
        {
            Token = (CancellationToken)t;

            while (Token.IsCancellationRequested == false)
            {
                ExecutionMethod();
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                DisposeMethod();
            }

            _disposed = true;
        }
    }
}