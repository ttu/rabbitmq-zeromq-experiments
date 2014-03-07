using NetMQ;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace Common.NetMQ
{
    public class Pub<TRequest> : RunBase, IPub<TRequest>
        where TRequest : CommonRequest
    {
        private NetMQContext _zmqContext;
        private NetMQSocket _zmqSocket;

        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        private string _endPoint = string.Empty;

        public Pub(string endPoint)
        {
            _endPoint = endPoint;
        }

        protected override void StartMethod()
        {
            _zmqContext = NetMQContext.Create();
            _zmqSocket = _zmqContext.CreatePublisherSocket();
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

                var envelope = new NetMQFrame(Encoding.UTF8.GetBytes(request.Topic));
                var body = new NetMQFrame(request.ToByteArray());

                var msq = new NetMQMessage();
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