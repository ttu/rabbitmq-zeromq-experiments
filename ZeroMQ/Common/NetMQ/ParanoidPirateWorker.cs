using NetMQ;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.NetMQ
{
    public class ParanoidPirateWorker
    {
        private const int INTERVAL_INIT = 1000;  // Initial reconnect
        private const int INTERVAL_MAX = 32000;  // After exponential back off

        private NetMQContext _context;
        private NetMQSocket _worker;

        private NetMQScheduler _scheduler;
        private NetMQTimer _heartbeatTimer;
        private Poller _poller;

        private DateTime _nextHeartbeatAt;

        private int _interval;
        private int _liveness;
        private int _cylces;

        public ParanoidPirateWorker()
        {
            _interval = INTERVAL_INIT;
            _liveness = Paranoid.HEARTBEAT_LIVENESS;

            _context = NetMQContext.Create();
            _worker = _context.CreateDealerSocket();
            _worker.Options.Identity = Encoding.Unicode.GetBytes(Guid.NewGuid().ToString());

            var shortId = Guid.Parse(Encoding.Unicode.GetString(_worker.Options.Identity)).ToPrintable();
            Console.WriteLine("Worker: {0}", shortId);
        }

        public void Start()
        {
            _worker.ReceiveReady += _worker_ReceiveReady;
            _worker.Connect("tcp://localhost:5556");

            _heartbeatTimer = new NetMQTimer(Paranoid.HEARTBEAT_INTERVAL_MS);
            _heartbeatTimer.Elapsed += heartbeatTimer_Elapsed;

            _poller = new Poller();
            _poller.AddSocket(_worker);
            _poller.AddTimer(_heartbeatTimer);

            _scheduler = new NetMQScheduler(_context, _poller);

            _nextHeartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS);
            _cylces = 0;

            Task.Factory.StartNew(() => Run(), TaskCreationOptions.LongRunning);

            SendReady();
        }

        public void Stop()
        {
            _poller.Stop();
        }

        public void Dispose()
        {
            if (_poller != null)
            {
                _poller.RemoveSocket(_worker);
                _poller.RemoveTimer(_heartbeatTimer);
                _poller = null;
            }

            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Elapsed -= heartbeatTimer_Elapsed;
                _heartbeatTimer = null;
            }

            if (_scheduler != null)
            {
                //_scheduler.Dispose();
            }

            if (_worker != null)
            {
                _worker.ReceiveReady -= _worker_ReceiveReady;
                _worker.Disconnect("tcp://localhost:5556");
            }
        }

        private void Run()
        {
            _poller.Start();
        }

        private void heartbeatTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            // If liveness hits zero, queue is considered disconnected
            if (--_liveness <= 0)
            {
                Console.WriteLine("{0} - W: heartbeat failure, can't reach queue.", DateTime.Now.ToLongTimeString());
                Console.WriteLine("{0} - W: reconnecting in {1} msecs...", DateTime.Now.ToLongTimeString(), _interval);

                Thread.Sleep(_interval);

                // Exponential back off
                if (_interval < INTERVAL_MAX)
                    _interval *= 2;

                _liveness = Paranoid.HEARTBEAT_LIVENESS;

                // Break the while loop and start the connection over
                Task.Factory.StartNew(_ =>
                    {
                        Console.WriteLine("{0} - I: restart", DateTime.Now.ToLongTimeString());

                        Stop();
                        Dispose();
                        Start();
                    }, _scheduler);
            }

            // Send heartbeat to queue if it's time
            if (DateTime.Now > _nextHeartbeatAt)
            {
                _nextHeartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL_MS);
                var heartbeatMessage = new NetMQMessage();
                heartbeatMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Paranoid.PPP_HEARTBEAT)));

                _worker.SendMessage(heartbeatMessage);
            }
        }

        private void _worker_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();

            var identity = e.Socket.Options.Identity;
            var content = Encoding.Unicode.GetString(message[0].Buffer);

            switch (content)
            {
                case Paranoid.PPP_HEARTBEAT:
                    _interval = INTERVAL_INIT;
                    _liveness = Paranoid.HEARTBEAT_LIVENESS;
                    //Console.WriteLine(DateTime.Now.ToLongTimeString() + " - W: heartbeat received");
                    break;

                default:
                    if (message.FrameCount > 1)
                    {
                        // NOTE: Rep has message payload in [2], Dealer in [1]
                        var text = Encoding.Unicode.GetString(message[1].Buffer);
                        // var text = Encoding.Unicode.GetString(message[2].Buffer);
                        Console.WriteLine("{0} - W: from {1}", DateTime.Now.ToLongTimeString(), Guid.Parse(content).ToPrintable());
                        Console.WriteLine("{0} - W: {1}", DateTime.Now.ToLongTimeString(), text);

                        if (!DoWork(_cylces++))
                        {
                            Stop();
                            break;
                        }

                        _interval = INTERVAL_INIT;
                        _liveness = Paranoid.HEARTBEAT_LIVENESS;
                        Console.WriteLine("{0} - W: Completed", DateTime.Now.ToLongTimeString());
                        _worker.SendMessage(message);
                    }
                    else
                    {
                        var id = Guid.Parse(Encoding.Unicode.GetString(identity)).ToPrintable();
                        Console.WriteLine("{0} - E: invalid message {1}", DateTime.Now.ToLongTimeString(), id);
                    }

                    break;
            };
        }

        private void SendReady()
        {
            // Tell the queue we're ready for work
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " - I: worker ready");

            var message = new NetMQMessage();
            message.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Paranoid.PPP_READY)));

            _worker.SendMessage(message);
        }

        private bool DoWork(int cycle)
        {
            var rand = new Random((int)DateTime.Now.Ticks);

            if (cycle > 3 && rand.Next(20) == 0)
            {
                Console.WriteLine("{0} - I: simulating a crash", DateTime.Now.ToLongTimeString());
                return false;
            }
            else if (cycle > 1 && rand.Next(8) == 0)
            {
                Console.WriteLine("{0} - I: simulating a CPU overload", DateTime.Now.ToLongTimeString());
                Thread.Sleep(6000);
            }

            Thread.Sleep(3000);

            return true;
        }
    }
}