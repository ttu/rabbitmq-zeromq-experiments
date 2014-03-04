using System;
using System.Diagnostics;
using System.Text;
using ZeroMQ;

namespace Common
{
    public class Sub<TRequest> : RunBase
    {
        private ZmqContext _zmqContext;
        private ZmqSocket _zmqSocket;

        private string _endPoint;
        private string _subsPrefix;

        private Func<TRequest, bool> _action;

        public Sub(string endPoint, string subsPrefix, Func<TRequest, bool> action)
        {
            _endPoint = endPoint;
            _subsPrefix = subsPrefix;
            _action = action;
        }

        protected override void StartMethod()
        {
            _zmqContext = ZmqContext.Create();
            _zmqSocket = _zmqContext.CreateSocket(SocketType.SUB);

            if (string.IsNullOrEmpty(_subsPrefix))
                _zmqSocket.SubscribeAll();
            else
                _zmqSocket.Subscribe(Encoding.UTF8.GetBytes(_subsPrefix));

            _zmqSocket.Connect(_endPoint);
        }

        protected override void StopMethod()
        {
        }

        protected override void ExecutionMethod()
        {
            try
            {
                var msq = _zmqSocket.ReceiveMessage();
                var request = SerializationMethods.FromByteArray<TRequest>(msq[1].Buffer);
                _action(request);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }

        protected override void DisposeMethod()
        {
            _zmqSocket.Dispose();
            _zmqContext.Dispose();
        }
    }
}