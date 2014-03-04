using System;
using System.Collections.Concurrent;
using System.Text;
using ZeroMQ;

namespace Common
{
    public class Pub<TRequest> : RunBase 
        where TRequest : CommonRequest
    {
        private ZmqContext _zmqContext;
        private ZmqSocket _zmqSocket;

        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        private string _endPoint = string.Empty;

        public Pub(string endPoint)
        {
            _endPoint = endPoint;
        }

        protected override void StartMethod()
        {
            _zmqContext = ZmqContext.Create();
            _zmqSocket = _zmqContext.CreateSocket(SocketType.PUB);
            _zmqSocket.Bind(_endPoint);
        }

        protected override void StopMethod()
        {
        }

        protected override void ExecutionMethod()
        {
            try
            {
                 TRequest request = _requests.Take(Token);

                var envelope = new Frame(Encoding.UTF8.GetBytes(request.Topic));
                var body = new Frame(request.ToByteArray());

                ZmqMessage msq = new ZmqMessage();
                msq.Append(envelope);
                msq.Append(body);

                _zmqSocket.SendMessage(msq);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override void DisposeMethod()
        {
            _zmqSocket.Dispose();
            _zmqContext.Dispose();
        }

        public void AddMessage(TRequest msg)
        {
            _requests.Add(msg);
        }
    }
}