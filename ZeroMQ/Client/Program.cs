using Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ParanoidClient();
            //PubClient(true);
            //PushClient();

            //Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }

        public static void ParanoidClient()
        {
            var client = new Common.NetMQ.ParanoidPirateClient();
            client.Start();

            for (int i = 0; i < 20; i++)
            {
                var work = string.Format("Work number [{0}]", i);
                client.Send(work);
                Console.WriteLine("{1} - I: sent {0}", work, DateTime.Now.ToLongTimeString());
                Thread.Sleep(1000);
            }
        }

        private static void PubClient(bool useNetMQ)
        {
            int msgCount = 10;
            Guid clientId = Guid.NewGuid();

            var messagesToSend = new Queue<CommonRequest>();
            var receivedMessages = new List<int>();

            for (int i = 0; i < msgCount; i++)
            {
                messagesToSend.Enqueue(new CommonRequest
                {
                    ClientId = clientId,
                    RequestId = i,
                    Topic = i % 2 == 0 ? "ABBA" : "ACDC",
                    Message = "Hello: " + i,
                    Duration = 5000
                });
            }

            Console.WriteLine("Client {0}", clientId.ToString().Substring(30));

            IPub<CommonRequest> pub = null;

            if (useNetMQ)
            {
                pub = new Common.NetMQ.Pub<CommonRequest>("tcp://127.0.0.1:5020");
            }
            else
            {
                pub = new Common.clrzmq.Pub<CommonRequest>("tcp://127.0.0.1:5020");
            }

            pub.Start();

            var senderTask = Task.Factory.StartNew(() =>
              {
                  while (messagesToSend.Count > 0)
                  {
                      Thread.Sleep(2000);
                      var msg = messagesToSend.Dequeue();
                      Console.WriteLine("Sending {0}", msg.RequestId.ToString());
                      pub.AddMessage(msg);
                  }
              });

            Task.WaitAll(senderTask);
        }

        private static void PushClient()
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

            var fromBroker = new Func<CommonRequest, bool>(r =>
            {
                Console.WriteLine("[x] Received to {0}", r.ClientId);
                return true;
            });

            var senderTask = Task.Factory.StartNew(() =>
            {
                var push = new Common.clrzmq.Push<CommonRequest>("tcp://127.0.0.1:5001");

                var fillTask = Task.Factory.StartNew(() =>
                {
                    foreach (var msg in messagesToSend)
                    {
                        push.AddMessage(msg);
                        Console.WriteLine("[x] Sent {0}", msg.RequestId);
                        Thread.Sleep(2000);
                    }

                    Console.WriteLine("Sent all");
                });

                push.Start();
            });

            var receiverTask = Task.Factory.StartNew(() =>
            {
                // TODO
            });

            while (receivedMessages.Count != messagesToSend.Count)
                Thread.Sleep(500);

            //Task.WaitAll(senderTask, receiverTask);
        }
    }
}