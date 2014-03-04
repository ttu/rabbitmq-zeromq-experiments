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
            PubClient();
            //PushClient();

            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }

        private static void PubClient()
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

            var push = new Pub<CommonRequest>("tcp://127.0.0.1:5020");
            push.Start();

            var senderTask = Task.Factory.StartNew(() =>
              {
                  while (messagesToSend.Count > 0)
                  {
                      Thread.Sleep(2000);
                      var msg = messagesToSend.Dequeue();
                      Console.WriteLine("Sending {0}", msg.RequestId.ToString());
                      push.AddMessage(msg);
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
                var push = new Push<CommonRequest>("tcp://127.0.0.1:5001");

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