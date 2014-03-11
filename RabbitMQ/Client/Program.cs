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
        public static void Main(string[] args)
        {
            if (args[0] == "s")
                ScatterGather(args.Skip(1).ToArray());
            else
                Normal();
        }

        private static void Normal()
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

            Console.WriteLine("Client {0}", clientId.ToString().Substring(30));

            var senderTask = Task.Factory.StartNew(() =>
            {
                // Broadcast to all
                //var producer = new ProducerWithExchange();

                // Send only to one at a time
                var producer = new Producer<CommonRequest>();

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
                var consumer = new Consumer<CommonReply>("localhost", qName);

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
                var rand = new Random();

                messagesToSend.Add(new CommonRequest
                {
                    ClientId = clientId,
                    RequestId = i,
                    Message = "Hello: " + i,
                    Duration = rand.Next(10) * 1000
                });
            }

            Console.WriteLine("Client {0}", clientId.ToString().Substring(30));

            var func = new Func<CommonReply, bool>(r =>
            {
                Console.WriteLine("[{1}] Received {0}", r.ReplyId, DateTime.Now.ToLongTimeString());

                receivedMessages.Add(r.ReplyId);

                if (receivedMessages.Count == messagesToSend.Count)
                    Console.WriteLine("Received all");

                return true;
            });

            var scatter = new ScatterGatherProducer<CommonRequest, CommonReply>(func);
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

            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }
    }
}