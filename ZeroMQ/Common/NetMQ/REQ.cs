using NetMQ;
using System;
using System.Collections.Concurrent;
using System.Text;

namespace Common.NetMQ
{
    public class REQ<TRequest, TResponse>
    {
        private NetMQContext _context;

        private string _bindEndPoint;

        public REQ(string endpoint)
        {
            _bindEndPoint = endpoint;
            _context = NetMQContext.Create();
        }

        public TResponse SendRequest(TRequest request)
        {
            using (var socket = _context.CreateRequestSocket())
            {
                socket.Connect(_bindEndPoint);

                var envelope = new NetMQFrame(Encoding.UTF8.GetBytes(request.ToString()));
                var body = new NetMQFrame(request.ToByteArray());

                var msq = new NetMQMessage();
                msq.Append(envelope);
                msq.Append(body);

                socket.SendMessage(msq);

                var responseMsg = socket.ReceiveMessage();

                return SerializationMethods.FromByteArray<TResponse>(responseMsg[3].Buffer);
            }
        }
    }
}