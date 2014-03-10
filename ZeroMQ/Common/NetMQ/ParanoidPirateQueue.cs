using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public static class Paranoid
    {
        public const int HEARTBEAT_LIVENESS = 3; //3-5 is reasonable
        public const int HEARTBEAT_INTERVAL = 1000; //msecs

        public const string PPP_READY = "READY";
        public const string PPP_HEARTBEAT = "HEARTBEAT";
    }

    public class Worker
    {
        private byte[] _address;
        private DateTime _expiry;

        public Worker(byte[] address)
        {
            _address = address;
            _expiry = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL * Paranoid.HEARTBEAT_LIVENESS);
        }

        public byte[] Address { get { return _address; } }

        public void ResetExpiry()
        {
            _expiry = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL * Paranoid.HEARTBEAT_LIVENESS); ;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(Worker))
            {
                return false;
            }
            else
            {
                return _address.SequenceEqual((obj as Worker).Address);
            }
        }

        public override int GetHashCode()
        {
            return _address.GetHashCode();
        }
    }

    public class ParanoidPirateQueue
    {
        private NetMQContext _context;
        private NetMQSocket _frontend;
        private NetMQSocket _backend;

        private Poller _poller;
        private NetMQScheduler _scheduler;

        private List<Worker> _workerQueue;
        private DateTime _heartbeatAt;

        public ParanoidPirateQueue()
        {
            _workerQueue = new List<Worker>();
        }

        public void Start()
        {
            _context = NetMQContext.Create();
            _frontend = _context.CreateRouterSocket();
            _backend = _context.CreateRouterSocket();

            _frontend.Bind("tcp://localhost:5555"); // For Clients
            _backend.Bind("tcp://localhost:5556"); // For Workers

            _frontend.ReceiveReady += _frontEnd_ReceiveReady;
            _backend.ReceiveReady += _backEnd_ReceiveReady;

            var heartbeatTimer = new NetMQTimer(Paranoid.HEARTBEAT_INTERVAL);
            heartbeatTimer.Elapsed += heartbeatTimer_Elapsed;

            _poller = new Poller();
            _poller.AddSocket(_frontend);
            _poller.AddSocket(_backend);
            _poller.AddTimer(heartbeatTimer);

            _scheduler = new NetMQScheduler(_context, _poller);

            _heartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL);

            Task.Factory.StartNew(() => Run(), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _poller.Stop();
        }

        private void Run()
        {
            _poller.Start();
        }

        private void heartbeatTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            //Send heartbeats to idle workers if it's time
            if (DateTime.Now >= _heartbeatAt)
            {
                foreach (var worker in _workerQueue)
                {
                    var heartbeatMessage = new NetMQMessage();
                    heartbeatMessage.Append(new NetMQFrame(worker.Address));
                    heartbeatMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Paranoid.PPP_HEARTBEAT)));

                    _backend.SendMessage(heartbeatMessage);
                }

                _heartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL);
            }
        }

        private void _frontEnd_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            //  Now get next client request, route to next worker
            //  Dequeue and drop the next worker address
            var message = e.Socket.ReceiveMessage();

            var worker = _workerQueue[0];
            message.Push(new NetMQFrame(worker.Address));

            _workerQueue.RemoveAt(0);

            _backend.SendMessage(message);
        }

        private void _backEnd_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();

            // TODO: What is message[0]?

            byte[] identity = message[1].Buffer;

            //Any sign of life from worker means it's ready, Only add it to the queue if it's not in there already
            Worker worker = null;

            if (_workerQueue.Count > 0)
            {
                var workers = _workerQueue.Where(x => x.Address.SequenceEqual(identity));

                if (workers.Any())
                    worker = workers.Single();
            }

            if (worker == null)
            {
                _workerQueue.Add(new Worker(identity));
            }

            //Return reply to client if it's not a control message
            switch (Encoding.Unicode.GetString(message[2].Buffer))
            {
                case Paranoid.PPP_READY:
                    Console.WriteLine("Worker " + Encoding.Unicode.GetString(identity) + " is ready…");
                    break;

                case Paranoid.PPP_HEARTBEAT: //Worker Refresh
                    if (worker != null)
                    {
                        worker.ResetExpiry();
                        Console.WriteLine("Worker " + Encoding.Unicode.GetString(identity) + " refresh");
                    }
                    else
                    {
                        Console.WriteLine("E: worker " + Encoding.Unicode.GetString(identity) + " not ready…");
                    }
                    break;

                default:
                    _frontend.SendMessage(message);
                    break;
            };
        }
    }
}