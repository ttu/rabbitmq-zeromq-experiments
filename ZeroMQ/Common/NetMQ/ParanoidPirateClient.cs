using NetMQ;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public class ParanoidPirateClient
    {
        private NetMQContext _context;
        private NetMQSocket _socket;
        private Poller _poller;

        private string _endPoint = string.Empty;

        public ParanoidPirateClient()
        {
            _endPoint = "tcp://localhost:5555";
        }

        public void Start()
        {
            _context = NetMQContext.Create();
            // Dealer can send any number of requests and just wait for the answers
            _socket = _context.CreateDealerSocket();
            // REP can send only one request and wait reply for that before sending more
            //_socket = _context.CreateRequestSocket();
            var clientId = Guid.NewGuid();
            _socket.Options.Identity = Encoding.Unicode.GetBytes(clientId.ToString());
            Console.WriteLine("Client: {0}", clientId.ToPrintable());

            _socket.Connect(_endPoint);

            _socket.ReceiveReady += _socket_ReceiveReady;

            _poller = new Poller();
            _poller.AddSocket(_socket);

            Task.Factory.StartNew(() => Run(), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _poller.Stop();
        }

        public void Send(string work)
        {
            var message = new NetMQMessage();
            message.Append(Encoding.Unicode.GetBytes(work));
            _socket.SendMessage(message);
        }

        private void Run()
        {
            _poller.Start();
            Console.WriteLine("Stopped!");
        }

        private void _socket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();

            var content = Encoding.Unicode.GetString(message[0].Buffer);
            Console.WriteLine("{0} - Received: {1}", DateTime.Now.ToLongTimeString(), content);
        }
    }
}