using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public class Sub<TRequest> : RunBase
    {
        private NetMQContext _zmqContext;
        private NetMQSocket _zmqSocket;

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
            _zmqContext = NetMQContext.Create();
            _zmqSocket = _zmqContext.CreateSubscriberSocket();

            if (string.IsNullOrEmpty(_subsPrefix))
                _zmqSocket.Subscribe(string.Empty);
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
