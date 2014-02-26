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

            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
        }
    }
}