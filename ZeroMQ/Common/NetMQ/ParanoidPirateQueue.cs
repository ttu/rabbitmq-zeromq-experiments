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
        public const int HEARTBEAT_LIVENESS = 5; // 3-5 is reasonable
        public const int HEARTBEAT_INTERVAL_MS = 1000;
        public const int HEARTBEAT_KILLTIME_MS = HEARTBEAT_INTERVAL_MS * HEARTBEAT_LIVENESS * 4;

        public const string PPP_READY = "READY";
        public const string PPP_HEARTBEAT = "HEARTBEAT";
    }

    public class ParanoidPirateQueue
    {
        private NetMQContext _context;
        private NetMQSocket _frontend;
        private NetMQSocket _backend;

        private Poller _poller;

        private List<WorkerInfo> _workerQueue;

        private DateTime _nextHeartbeatAt;

        private BlockingCollection<RequestInfo> _requests = new BlockingCollection<RequestInfo>();
        private List<RequestInfo> _sentRequests = new List<RequestInfo>();

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
                var request = _requests.Take(token);

                while (_workerQueue.Any(w => w.IsWorking == false) == false)
                    Thread.Sleep(100);

                WorkerInfo worker = _workerQueue.First(w => w.IsWorking == false);
                worker.IsWorking = true;

                request.AssignedTo = worker;
                request.Message.Push(new NetMQFrame(worker.Address));

                _backend.SendMessage(request.Message);

                lock (_sentRequests)
                    _sentRequests.Add(request);
            }
        }

        private void heartbeatTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            // Send heartbeats to idle workers if it's time
            if (DateTime.Now >= _nextHeartbeatAt)
            {
                var expired = _workerQueue.Where(w => w.Expiry < DateTime.Now).ToList();
                expired.ForEach(w => HandleExpiredWorker(w));

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
            var message = e.Socket.ReceiveMessage();

            var idString = Guid.NewGuid().ToString();

            // Add indentifier as last Frame
            var newMessage = new NetMQMessage();
            newMessage.Push(Encoding.Unicode.GetBytes(idString));

            foreach (var frame in message.Reverse())
            {
                newMessage.Push(frame);
            }

            var request = new RequestInfo { Id = idString, Message = newMessage };

            _requests.Add(request);
        }

        private void _backEnd_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();

            var workerIdentity = message.Pop().Buffer; // After this Client address is again on top (if there is one)
            var content = Encoding.Unicode.GetString(message[0].Buffer);

            var worker = _workerQueue.SingleOrDefault(x => x.Address.SequenceEqual(workerIdentity));

            switch (content)
            {
                case Paranoid.PPP_READY:
                    if (worker != null)
                    {
                        Console.WriteLine("{0} - Worker {1} already in queue", DateTime.Now.ToLongTimeString(), worker.ShortId);
                        break;
                    }

                    worker = new WorkerInfo(workerIdentity);
                    _workerQueue.Add(worker);
                    Console.WriteLine("{0} - Worker {1} is ready", DateTime.Now.ToLongTimeString(), worker.ShortId);
                    break;

                case Paranoid.PPP_HEARTBEAT:
                    if (worker != null)
                    {
                        worker.ResetExpiry();
                        //Console.WriteLine(DateTime.Now.ToLongTimeString() + " - Worker " + Encoding.Unicode.GetString(workerIdentity) + " refresh");
                    }
                    else
                    {
                        // This might happen when worker is just taken from queue so work is assigned to it
                        Console.WriteLine("{0} - E: worker {1} not in queue", DateTime.Now.ToLongTimeString(), worker.ShortId);
                    }

                    break;

                // Return reply to client if it's not a control message
                default:
                    worker.IsWorking = false;
                    _frontend.SendMessage(message);

                    var id = Encoding.Unicode.GetString(message.Last().Buffer);

                    lock (_sentRequests)
                        _sentRequests.Remove(_sentRequests.Single(s => s.Id == id));

                    break;
            };
        }

        private void HandleExpiredWorker(WorkerInfo w)
        {
            // TODO: Do not send messages to frontend with every heartbeat

            if (w.IsWorking)
            {
                // Worker might be just stuck, so send notification to client
                var messages = _sentRequests.Where(s => s.AssignedTo == w).ToList();

                // Check if it is time to announce worker dead
                if (w.Expiry.AddMilliseconds(Paranoid.HEARTBEAT_KILLTIME_MS) < DateTime.Now)
                {
                    Console.WriteLine("{0} - Worker {1} is lost and messages resent", DateTime.Now.ToLongTimeString(), w.ShortId);

                    _workerQueue.Remove(w);

                    messages.ForEach(m =>
                    {
                        var errorMessage = new NetMQMessage(new List<NetMQFrame>
                            {
                                m.Message[1],
                                new NetMQFrame(Encoding.Unicode.GetBytes("Resend")),
                                m.Message[2]
                            });
                        _frontend.SendMessage(errorMessage);

                        var id = Encoding.Unicode.GetString(m.Message.Last().Buffer);
                        RequestInfo info;

                        lock (_sentRequests)
                        {
                            info = _sentRequests.Single(s => s.Id == id);
                            _sentRequests.Remove(info);
                        }

                        info.AssignedTo = null;
                        // TODO: Should add to beginning of requests queue
                        // Remove worker address
                        info.Message.Pop();
                        _requests.Add(info);
                    });
                }
                else
                {
                    Console.WriteLine("{0} - Worker {1} is lost and still working", DateTime.Now.ToLongTimeString(), w.ShortId);

                    messages.ForEach(m =>
                    {
                        var errorMessage = new NetMQMessage(new List<NetMQFrame>
                            {
                                m.Message[1],
                                new NetMQFrame(Encoding.Unicode.GetBytes("Error")),
                                m.Message[2]
                            });
                        _frontend.SendMessage(errorMessage);
                    });
                }
            }
            else
            {
                Console.WriteLine("{0} - Worker {1} is lost", DateTime.Now.ToLongTimeString(), w.ShortId);

                _workerQueue.Remove(w);
            }
        }

        private class WorkerInfo
        {
            private byte[] _address;
            private DateTime _expiry;
            private string _shortId;

            public WorkerInfo(byte[] address)
            {
                _address = address;
                _expiry = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS * Paranoid.HEARTBEAT_LIVENESS);
                _shortId = Guid.Parse(Encoding.Unicode.GetString(_address)).ToPrintable();
            }

            public string ShortId { get { return _shortId; } }

            public byte[] Address { get { return _address; } }

            public DateTime Expiry { get { return _expiry; } }

            public bool IsWorking { get; set; }

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

        private class RequestInfo
        {
            public string Id { get; set; }

            public WorkerInfo AssignedTo { get; set; }

            public NetMQMessage Message { get; set; }
        }
    }
}