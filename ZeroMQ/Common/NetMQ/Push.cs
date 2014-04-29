using NetMQ;
using System.Text;

namespace Common.NetMQ
{
    public class Push<TData>
    {
        private NetMQContext _context;

        private string _bindEndPoint;

        public Push(string endpoint)
        {
            _bindEndPoint = endpoint;
            _context = NetMQContext.Create();
        }

        public void Send(TData request)
        {
            using (var socket = _context.CreatePushSocket())
            {
                socket.Connect(_bindEndPoint);

                var envelope = new NetMQFrame(Encoding.UTF8.GetBytes(request.ToString()));
                var body = new NetMQFrame(request.ToByteArray());

                var msq = new NetMQMessage();
                msq.Append(envelope);
                msq.Append(body);

                socket.SendMessage(msq);
            }
        }
    }
}