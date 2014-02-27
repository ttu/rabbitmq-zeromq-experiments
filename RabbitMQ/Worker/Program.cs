using Common;
using System;
using System.Threading;

namespace Worker
{
    internal class Program
    {
        public static void Main()
        {
            var workMethod = new Func<CommonRequest, bool>(r =>
            {
                var message = r.Message;
                Console.WriteLine("[{2}] Received from {0}, message {1}", r.ClientId.ToString().Substring(30), r.RequestId, DateTime.Now.ToLongTimeString());

                Console.WriteLine("[x] Working {0}ms", r.Duration);
                Thread.Sleep(r.Duration);

                Console.WriteLine("[x] Done");

                var qName = string.Format("{0}_queue", r.ClientId.ToString());

                var publisher = new Producer<CommonReply>("localhost", qName);
                publisher.Publish(new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true });

                return true;
            });

            Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

            //var consumer = new Consumer<CommonRequest>();
            //consumer.Start(workMethod);

            // Now all will receive
            //var consumer = new ConsumerWithExchange();
            //consumer.Start(workMethod);

            var scatterWorkMethod = new Func<CommonRequest, CommonReply>(r =>
            {
                var message = r.Message;
                Console.WriteLine("[{2}] Received from {0}, message {1}", r.ClientId.ToString().Substring(30), r.RequestId, DateTime.Now.ToLongTimeString());

                Console.WriteLine("[x] Working {0}ms", r.Duration);
                Thread.Sleep(r.Duration);

                Console.WriteLine("[x] Done");

                return new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true };
            });

            // Consumer with ScatterGather
            var consumer = new ScatterGatherConsumer<CommonRequest, CommonReply>(scatterWorkMethod);
            consumer.Start();
        }
    }
}