using NetMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public static class Paranoid
    {
        public const int HEARTBEAT_LIVENESS = 3; // 3-5 is reasonable
        public const int HEARTBEAT_INTERVAL_MS = 3000;

        public const string PPP_READY = "READY";
        public const string PPP_HEARTBEAT = "HEARTBEAT";
    }

    public class ParanoidPirateQueue
    {
        private NetMQContext _context;
        private NetMQSocket _frontend;
        private NetMQSocket _backend;

        private Poller _poller;
        //private NetMQScheduler _scheduler;

        private List<WorkerInfo> _workerQueue;
        private DateTime _nextHeartbeatAt;

        private BlockingCollection<NetMQMessage> _requests = new BlockingCollection<NetMQMessage>();
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public ParanoidPirateQueue()
        {
            _workerQueue = new List<WorkerInfo>();
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

            var heartbeatTimer = new NetMQTimer(Paranoid.HEARTBEAT_INTERVAL_MS);
            heartbeatTimer.Elapsed += heartbeatTimer_Elapsed;

            _poller = new Poller();
            _poller.AddSocket(_frontend);
            _poller.AddSocket(_backend);
            _poller.AddTimer(heartbeatTimer);

            //_scheduler = new NetMQScheduler(_context, _poller);

            _nextHeartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS);

            Task.Factory.StartNew(t => Run(t), _tokenSource.Token, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(_poller.Start, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _poller.Stop();
        }

        private void Run(object t)
        {
            var token = (CancellationToken)t;

            while (token.IsCancellationRequested == false)
            {
                while (_workerQueue.Count == 0)
                    Thread.Sleep(100);

                var message = _requests.Take(token);

                var worker = _workerQueue[0];
                _workerQueue.RemoveAt(0);

                message.Push(new NetMQFrame(worker.Address));

                _backend.SendMessage(message);
            }
        }

        private void heartbeatTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            // Send heartbeats to idle workers if it's time
            if (DateTime.Now >= _nextHeartbeatAt)
            {
                foreach (var worker in _workerQueue)
                {
                    var heartbeatMessage = new NetMQMessage();
                    heartbeatMessage.Append(new NetMQFrame(worker.Address));
                    heartbeatMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Paranoid.PPP_HEARTBEAT)));

                    _backend.SendMessage(heartbeatMessage);
                }

                _nextHeartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS);
            }
        }

        private void _frontEnd_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            // Now get next client request, route to next worker
            // Dequeue and drop the next worker address
            var message = e.Socket.ReceiveMessage();

            _requests.Add(message);

            // TODO: Add messages to queue
        }

        private void _backEnd_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();

            var workerIdentity = message.Pop().Buffer; // After this Client address is again on top

            var content = Encoding.Unicode.GetString(message[0].Buffer);

            // Any sign of life from worker means it's ready, Only add it to the queue if it's not in there already
            WorkerInfo worker = null;

            if (_workerQueue.Count > 0)
            {
                var workers = _workerQueue.Where(x => x.Address.SequenceEqual(workerIdentity));

                if (workers.Any())
                    worker = workers.Single();
            }

            if (worker == null)
            {
                _workerQueue.Add(new WorkerInfo(workerIdentity));
            }

            // Return reply to client if it's not a control message
            switch (content)
            {
                case Paranoid.PPP_READY:
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + " - Worker " + Encoding.Unicode.GetString(workerIdentity) + " is ready");
                    break;

                case Paranoid.PPP_HEARTBEAT:
                    if (worker != null)
                    {
                        worker.ResetExpiry();
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " - Worker " + Encoding.Unicode.GetString(workerIdentity) + " refresh");
                    }
                    else
                    {
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " - E: worker " + Encoding.Unicode.GetString(workerIdentity) + " not read");
                    }
                    break;

                default:
                    _frontend.SendMessage(message);
                    break;
            };
        }

        private class WorkerInfo
        {
            private byte[] _address;
            private DateTime _expiry;

            public WorkerInfo(byte[] address)
            {
                _address = address;
                _expiry = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS * Paranoid.HEARTBEAT_LIVENESS);
            }

            public byte[] Address { get { return _address; } }

            public void ResetExpiry()
            {
                _expiry = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS * Paranoid.HEARTBEAT_LIVENESS); ;
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() != typeof(WorkerInfo))
                {
                    return false;
                }
                else
                {
                    return _address.SequenceEqual((obj as WorkerInfo).Address);
                }
            }

            public override int GetHashCode()
            {
                return _address.GetHashCode();
            }
        }
    }
}