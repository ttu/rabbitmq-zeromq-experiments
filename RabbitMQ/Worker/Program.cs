using Common;
using System;
using System.Linq;
using System.Threading;

namespace Worker
{
    internal class Program
    {
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
                Scatter(args.Skip(1).ToArray());
            else if (args[0] == "c")
                ClientSpecificWorkers(args.Skip(1).ToArray());
        }

        private static void ClientSpecificWorkers(string[] args)
        {
        }

        private static void Scatter(string[] args)
        {
            var scatterWorkMethod = new Func<CommonRequest, CommonReply>(r =>
            {
                var message = r.Message;
                Console.WriteLine("[{2}] Received from {0}, message {1}", r.ClientId.ToPrintable(), r.RequestId, DateTime.Now.ToLongTimeString());

                Console.WriteLine("[x] Working {0}ms", r.Duration);
                Thread.Sleep(r.Duration);

                Console.WriteLine("[x] Done");

                return new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true };
            });

            // Consumer with ScatterGather
            var consumer = new Common.ScatterGather.Consumer<CommonRequest, CommonReply>(scatterWorkMethod);
            consumer.Start();
        }

        private static void SharedWorker()
        {
            var workMethod = new Func<CommonRequest, bool>(r =>
            {
                var message = r.Message;
                Console.WriteLine("[{2}] Received from {0}, message {1}", r.ClientId.ToPrintable(), r.RequestId, DateTime.Now.ToLongTimeString());

                Console.WriteLine("[x] Working {0}ms", r.Duration);
                Thread.Sleep(r.Duration);

                Console.WriteLine("[x] Done");

                var qName = string.Format("{0}_queue", r.ClientId.ToString());

                var publisher = new Common.SharedWorker.Producer<CommonReply>("localhost", qName);
                publisher.Publish(new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true });

                return true;
            });

            Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

            //var consumer = new Common.PubSub.Consumer();
            var consumer = new Common.SharedWorker.Consumer<CommonRequest>();

            consumer.Start(workMethod);
        }

        private static void PubSub()
        {
            var workMethod = new Func<CommonRequest, bool>(r =>
            {
                var message = r.Message;
                Console.WriteLine("[{2}] Received from {0}, message {1}", r.ClientId.ToPrintable(), r.RequestId, DateTime.Now.ToLongTimeString());

                Console.WriteLine("[x] Working {0}ms", r.Duration);
                Thread.Sleep(r.Duration);

                Console.WriteLine("[x] Done");

                return true;
            });

            Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

            var consumer = new Common.PubSub.Consumer();
            consumer.Start(workMethod);
        }
    }
}