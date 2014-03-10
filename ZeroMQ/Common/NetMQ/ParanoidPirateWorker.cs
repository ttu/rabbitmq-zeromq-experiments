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
        private const int INTERVAL_MAX = 32000;  // After exponential backoff

        private NetMQContext _context;
        private NetMQSocket _worker;

        private NetMQScheduler _scheduler;
        private NetMQTimer _heartbeatTimer;
        private Poller _poller;

        private DateTime _heartbeatAt;
        private byte[] _address;

        private int _interval;
        private int _liveness;
        private int _cylces;

        private AutoResetEvent _are = new AutoResetEvent(false);

        public ParanoidPirateWorker()
        {
            _address = Encoding.Unicode.GetBytes(Guid.NewGuid().ToString());
            _interval = INTERVAL_INIT;
            _liveness = Paranoid.HEARTBEAT_LIVENESS;

            _context = NetMQContext.Create();
            _worker = _context.CreateDealerSocket();
            _worker.Options.Identity = _address;

            _heartbeatTimer = new NetMQTimer(Paranoid.HEARTBEAT_INTERVAL);
            _heartbeatTimer.Elapsed += heartbeatTimer_Elapsed;
        }

        void _worker_SendReady(object sender, NetMQSocketEventArgs e)
        {           
        }

        public void Start()
        {
            _worker.Connect("tcp://localhost:5556");
            _worker.ReceiveReady += _worker_ReceiveReady;

            _poller = new Poller();
            _poller.AddSocket(_worker);
            _poller.AddTimer(_heartbeatTimer);

            _scheduler = new NetMQScheduler(_context, _poller);

            _heartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL);
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
            //if (_scheduler != null)
            //    _scheduler.Dispose();
            if (_worker != null)
                _worker.Disconnect("tcp://localhost:5556");
            //if (_context != null)
            //    _context.Dispose();
        }

        private void Run()
        {
            Console.WriteLine("I: Poller started");

            _poller.Start();

            Console.WriteLine("I: Poller stopped");
        }

        private void heartbeatTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            //If liveness hits zero, queue is considered disconnected
            if (--_liveness <= 0)
            {
                Console.WriteLine("W: heartbeat failure, can't reach queue.");
                Console.WriteLine("W: reconnecting in {0} msecs…", _interval);

                try
                {
                    Thread.Sleep(_interval);
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                //Exponential Backoff
                if (_interval < INTERVAL_MAX)
                    _interval *= 2;

                _liveness = Paranoid.HEARTBEAT_LIVENESS;

                //Break the while loop and start the connection over
                Task.Factory.StartNew(_ =>
                    {
                        try
                        {
                            //Stop();
                            //Dispose();
                            //Start();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }, _scheduler);
            }

            //Send heartbeat to queue if it's time
            if (DateTime.Now > _heartbeatAt)
            {
                _heartbeatAt = DateTime.Now.AddMilliseconds(Paranoid.HEARTBEAT_INTERVAL);
                var heartbeatMessage = new NetMQMessage();
                heartbeatMessage.Append(new NetMQFrame(_address));
                heartbeatMessage.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Paranoid.PPP_HEARTBEAT)));

                _worker.SendMessage(heartbeatMessage);
            }
        }

        private void _worker_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMessage();

            // TODO: What is message[0]?

            byte[] identity = e.Socket.Options.Identity;

            switch (Encoding.Unicode.GetString(message[0].Buffer))
            {
                case Paranoid.PPP_HEARTBEAT:
                    _interval = INTERVAL_INIT;
                    _liveness = Paranoid.HEARTBEAT_LIVENESS;
                    Console.WriteLine("W: heartbeat received");

                    break;

                default:
                    if (message.FrameCount > 1)
                    {
                        Thread.Sleep(3000);
                        //if (!doTheWork(_cylces++))
                        //{
                        //    break;
                        //}
                        _interval = INTERVAL_INIT;
                        _liveness = Paranoid.HEARTBEAT_LIVENESS;
                        Console.WriteLine("W: work completed");
                        _worker.SendMessage(message);
                    }
                    else
                    {
                        Console.WriteLine("E: invalied message {0}", identity);
                    }
                    break;
            };
        }

        private void SendReady()
        {
            //Tell the queue we're ready for work
            Console.WriteLine("I: worker ready");

            var message = new NetMQMessage();
            message.Append(new NetMQFrame(_address));
            message.Append(new NetMQFrame(Encoding.Unicode.GetBytes(Paranoid.PPP_READY)));

            _worker.SendMessage(message);
        }

        private static bool doTheWork(int cycle)
        {
            Random rand = new Random();

            try
            {
                if (cycle > 3 && rand.Next(6) == 0)
                {
                    Console.WriteLine("I: simulating a crash");
                    return false;
                }
                else if (cycle > 3 && rand.Next(6) == 0)
                {
                    Console.WriteLine("I: simulating a CPU overload");
                    Thread.Sleep(3000);
                }

                //Do some work
                Thread.Sleep(300);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return true;
        }
    }
}