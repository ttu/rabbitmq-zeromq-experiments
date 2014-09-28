using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        // ScatterGather; 10 messages; 2000ms sleep between messages
        // s 10 2000
        // ClientSpecificWorkers; 10 messages; 2000ms sleep between messages; 2 clients
        // c 10 2000 2
        public static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                //PubSub();
                SharedWorker();
            }
            else if (args[0] == "p")
                PubSub();
            else if (args[0] == "sw")
                SharedWorker();
            else if (args[0] == "s")
                ScatterGather(args.Skip(1).ToArray());
            else if (args[0] == "c")
                ClientSpecificWorkers(args.Skip(1).ToArray());
        }

        private static void PubSub()
        {
            int msgCount = 10;
            Guid clientId = Guid.NewGuid();

            var messagesToSend = new List<CommonRequest>();
            var receivedMessages = new List<int>();

            for (int i = 0; i < msgCount; i++)
            {
                messagesToSend.Add(new CommonRequest
                {
                    ClientId = clientId,
                    RequestId = i,
                    Message = "Hello: " + i,
                    Duration = 5000
                });
            }

            Console.WriteLine("Client {0}", clientId.ToPrintable());

            var senderTask = Task.Factory.StartNew(() =>
            {
                // Broadcast to all
                var producer = new Common.PubSub.Producer();

                foreach (var msg in messagesToSend)
                {
                    producer.Publish(msg);
                    Console.WriteLine("[x] Sent {0}", msg.RequestId);
                    Thread.Sleep(2000);
                }

                Console.WriteLine("Sent all");
            });

            Task.WaitAll(senderTask);

            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }

        private static void SharedWorker()
        {
            int msgCount = 10;
            Guid clientId = Guid.NewGuid();

            var messagesToSend = new List<CommonRequest>();
            var receivedMessages = new List<int>();

            for (int i = 0; i < msgCount; i++)
            {
                messagesToSend.Add(new CommonRequest
                {
                    ClientId = clientId,
                    RequestId = i,
                    Message = "Hello: " + i,
                    Duration = 5000
                });
            }

            Console.WriteLine("Client {0}", clientId.ToPrintable());

            var senderTask = Task.Factory.StartNew(() =>
            {
                // Broadcast to all
                //var producer = new Common.PubSub.Producer();

                // Send only to one at a time
                var producer = new Common.SharedWorker.Producer<CommonRequest>();

                foreach (var msg in messagesToSend)
                {
                    producer.Publish(msg);
                    Console.WriteLine("[x] Sent {0}", msg.RequestId);
                    Thread.Sleep(2000);
                }

                Console.WriteLine("Sent all");
            });

            var receiverTask = Task.Factory.StartNew(() =>
            {
                var qName = string.Format("{0}_queue", clientId.ToString());
                var consumer = new Common.SharedWorker.Consumer<CommonReply>("localhost", qName);

                var func = new Func<CommonReply, bool>(r =>
                {
                    Console.WriteLine("[{1}] Received {0}", r.ReplyId, DateTime.Now.ToLongTimeString());

                    receivedMessages.Add(r.ReplyId);

                    if (receivedMessages.Count == messagesToSend.Count)
                        consumer.Stop();

                    return true;
                });

                consumer.Start(func);
            });

            while (receivedMessages.Count != messagesToSend.Count)
                Thread.Sleep(500);

            //Task.WaitAll(senderTask, receiverTask);

            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }

        private static void ScatterGather(string[] args)
        {
            int msgCount = System.Convert.ToInt32(args[0]);
            int sleepTimeBetweenSends = System.Convert.ToInt32(args[1]);

            Guid clientId = Guid.NewGuid();

            var messagesToSend = new List<CommonRequest>();
            var receivedMessages = new List<int>();

            for (int i = 0; i < msgCount; i++)
            {
                Thread.Sleep(1);
                var rand = new Random((int)DateTime.Now.Ticks);

                messagesToSend.Add(new CommonRequest
                {
                    ClientId = clientId,
                    RequestId = i,
                    Message = "Hello: " + i,
                    Duration = rand.Next(10) * 1000
                });
            }

            Console.WriteLine("Client {0}", clientId.ToPrintable());

            var func = new Func<CommonReply, bool>(r =>
            {
                Console.WriteLine("[{1}] Received {0}", r.ReplyId, DateTime.Now.ToLongTimeString());

                receivedMessages.Add(r.ReplyId);

                if (receivedMessages.Count == messagesToSend.Count)
                    Console.WriteLine("Received all");

                return true;
            });

            var scatter = new Common.ScatterGather.Producer<CommonRequest, CommonReply>(func);
            scatter.Start();

            var senderTask = Task.Factory.StartNew(() =>
            {
                foreach (var msg in messagesToSend)
                {
                    scatter.Send(msg);
                    Console.WriteLine("[x] Sent {0}", msg.RequestId);
                    Thread.Sleep(sleepTimeBetweenSends);
                }

                Console.WriteLine("Sent all");
            });

            //Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }

        private static void ClientSpecificWorkers(string[] args)
        {
            int msgCount = System.Convert.ToInt32(args[0]);
            int sleepTimeBetweenSends = System.Convert.ToInt32(args[1]);
            int clientCount = System.Convert.ToInt32(args[2]);

            var messagesToSend = new Dictionary<Guid, List<CommonRequest>>();
            var receivedMessages = new Dictionary<Guid, List<int>>();

            var consumerFunc = new Func<CommonRequest, CommonReply>(r =>
            {
                var message = r.Message;
                var client = r.ClientId.ToPrintable();

                Console.WriteLine("[C] {3} ({0}) Working {1} ({2}ms)", client, r.RequestId, r.Duration, DateTime.Now.ToLongTimeString());
                Thread.Sleep(r.Duration);

                return new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true };
            });

            var reg = new Common.ClientSpecificWorkers.ClientRegister<CommonRequest, CommonReply>(consumerFunc);

            var receivedFunc = new Func<CommonReply, bool>(r =>
            {
                Console.WriteLine("[P] {2} ({0}) Received {1}", r.ClientId.ToPrintable(), r.ReplyId, DateTime.Now.ToLongTimeString());

                receivedMessages[r.ClientId].Add(r.ReplyId);

                if (receivedMessages[r.ClientId].Count == messagesToSend[r.ClientId].Count)
                {
                    Console.WriteLine("[P] {1} ({0}) Received all", r.ClientId.ToPrintable(), DateTime.Now.ToLongTimeString());
                    reg.ClientOffline(r.ClientId);
                }

                return true;
            });

            var sentFunc = new Action<CommonRequest>(message =>
            {
                Console.WriteLine("[P] {2} ({0}) Sent {1}", message.ClientId.ToPrintable(), message.RequestId, DateTime.Now.ToLongTimeString());
            });

            var producer = new Common.ClientSpecificWorkers.Producer<CommonRequest, CommonReply>(sentFunc, receivedFunc);
            producer.Start();

            for (int i = 0; i < clientCount; i++)
            {
                var clientId = Guid.NewGuid();
                messagesToSend.Add(clientId, new List<CommonRequest>());
                receivedMessages.Add(clientId, new List<int>());
                reg.ClientOnline(clientId);

                for (int j = 0; j < msgCount; j++)
                {
                    Thread.Sleep(1);
                    var rand = new Random((int)DateTime.Now.Ticks);

                    messagesToSend[clientId].Add(new CommonRequest
                    {
                        ClientId = clientId,
                        RequestId = j,
                        Message = "Hello: " + i,
                        Duration = rand.Next(10) * 1000
                    });
                }
            }

            for (int i = 0; i < clientCount; i++)
            {
                var current = i;

                var task = Task.Factory.StartNew(() =>
                    {
                        foreach (var msg in messagesToSend.Skip(current).First().Value)
                        {
                            producer.Send(msg);
                            Thread.Sleep(sleepTimeBetweenSends);
                        }
                    });
            }

            Console.ReadLine();

            producer.Stop();
        }
    }
}