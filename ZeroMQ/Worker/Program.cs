using Common;
using System;
using System.Threading;

namespace Worker
{
    internal class Program
    {
        public static void Main()
        {
            Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

            SubWorker();
            //ReqWorker();

            Console.ReadLine();
        }

        private static void SubWorker()
        {
            var workMethod = new Func<CommonRequest, bool>(r =>
            {
                Console.WriteLine("[*] Received from {0} request: {1}", r.ClientId.ToString().Substring(30), r.RequestId);
                return true;
            });

            var sub = new Sub<CommonRequest>("tcp://127.0.0.1:5020", "ACDC",  workMethod);
            sub.Start();
        }

        private static void ReqWorker()
        {
            var workMethod = new Func<CommonRequest, CommonReply>(r =>
            {
                Console.WriteLine("[*] Received from {0} request: {1}", r.ClientId.ToString().Substring(30), r.RequestId);
                Thread.Sleep(r.Duration);

                var reply = new CommonReply { ClientId = r.ClientId, ReplyId = r.RequestId, Success = true };
                return reply;
            });

            var req = new REQ<CommonRequest, CommonReply>("tcp://127.0.0.1:5000", workMethod);
            req.Start(new CommonReply() { Success = true });
        }
    }
}