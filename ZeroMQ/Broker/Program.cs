using Common;
using Common.clrzmq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Broker
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("[*] Running the Broker. To exit press CTRL+C");

            ParanoidBroker();
            //SingleBroker();
            //PullRepBroker();

            //Console.WriteLine("All tasks finished!");
            Console.ReadLine();
        }

        public static void ParanoidBroker()
        {
            var brokerTask = Task.Factory.StartNew(() =>
            {
                var broker = new Common.NetMQ.ParanoidPirateQueue();
                broker.Start();
            });

            Task.WaitAll(brokerTask);
        }

        private static void SingleBroker()
        {
            var responsesToClient = new List<CommonReply>();

            var processClientRequest = new Func<CommonRequest, bool>(r =>
            {
                Console.WriteLine("[x] Received request {0} from {1} ", r.RequestId, r.ClientId.ToString().Substring(30));
                return true;
            });

            var processWorkerResponse = new Func<CommonReply, bool>(r =>
            {
                Console.WriteLine("[x] Received reply {0} to {1} ", r.ReplyId, r.ClientId.ToString().Substring(30));
                // TODO: Send response to client
                return true;
            });

            var brokerTask = Task.Factory.StartNew(() =>
            {
                var broker = new PullRepBroker<CommonRequest, CommonReply>("tcp://127.0.0.1:5001", "tcp://127.0.0.1:5000", processClientRequest, processWorkerResponse);
                broker.Start();
            });

            Task.WaitAll(brokerTask);
        }

        private static void PullRepBroker()
        {
            var tasksToSend = new List<CommonRequest>();
            var responsesToClient = new List<CommonReply>();

            Pull<CommonRequest> pull = null;
            REP<CommonRequest, CommonReply> rep = null;

            var processClientRequest = new Func<CommonRequest, bool>(r =>
            {
                Console.WriteLine("[x] Received request {0} from {1} ", r.RequestId, r.ClientId.ToString().Substring(30));

                tasksToSend.Add(r);
                rep.AddTask(r);
                return true;
            });

            var fromClientTask = Task.Factory.StartNew(() =>
            {
                pull = new Pull<CommonRequest>("tcp://127.0.0.1:5001", processClientRequest);
                pull.Start();
            });

            var processWorkerResponse = new Func<CommonReply, bool>(r =>
             {
                 Console.WriteLine("[x] Received reply {0} to {1} ", r.ReplyId, r.ClientId.ToString().Substring(30));

                 if (r.ClientId != default(Guid))
                     responsesToClient.Add(r);

                 // TODO: Send response to client

                 return true;
             });

            var toWorkerTask = Task.Factory.StartNew(() =>
                {
                    rep = new REP<CommonRequest, CommonReply>("tcp://127.0.0.1:5000", processWorkerResponse);
                    rep.Start();
                });

            Task.WaitAll(fromClientTask, toWorkerTask);
        }
    }
}