using NetMQ;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public class SubWithPoller<TRequest>
    {
        private NetMQContext _zmqContext;
        private NetMQSocket _zmqSocket;
        private Poller _poller;

        private string _endPoint = string.Empty;
        private string _subsPrefix;

        private Func<TRequest, bool> _action;

        public SubWithPoller(string endPoint, string subsPrefix, Func<TRequest, bool> action)
        {
            _endPoint = endPoint;
            _subsPrefix = subsPrefix;
            _action = action;
        }

        public void Start()
        {
            _zmqContext = NetMQContext.Create();
            _zmqSocket = _zmqContext.CreateSubscriberSocket();

            if (string.IsNullOrEmpty(_subsPrefix))
                _zmqSocket.Subscribe(string.Empty);
            else
                _zmqSocket.Subscribe(Encoding.UTF8.GetBytes(_subsPrefix));

            _zmqSocket.Connect(_endPoint);

            _zmqSocket.ReceiveReady += _zmqSocket_ReceiveReady;

            _poller = new Poller();
            _poller.AddSocket(_zmqSocket);

            Task.Factory.StartNew(() => Run(), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _poller.Stop();
        }

        private void Run()
        {
            _poller.Start();

            Console.WriteLine("Stopped!");
        }

        private void _zmqSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msq = e.Socket.ReceiveMessage();
            var request = SerializationMethods.FromByteArray<TRequest>(msq[1].Buffer);
            _action(request);
        }
    }
}