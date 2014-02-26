using Common;
using System;
using System.Threading;

namespace Worker
{
    internal class Program
    {
        public static void Main()
        {
            var workMethod = new Func<CommonRequest, CommonReply>(r =>
            {
                Console.WriteLine("[*] Received from {0} request: {1}", r.ClientId.ToString().Substring(30), r.RequestId);
                Thread.Sleep(r.Duration);

                var reply = new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true };
                return reply;
            });

            Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

            var req = new REQ<CommonRequest, CommonReply>("tcp://127.0.0.1:5000", workMethod);
            req.Start(new CommonReply() { Success = true });

            // Now all will receive
            //var consumer = new ConsumerWithExchange();
            //consumer.Start(workMethod);
        }
    }
}