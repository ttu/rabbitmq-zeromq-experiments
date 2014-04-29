using Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using Worker.ExampleApplication;

namespace Worker
{
    internal class Program
    {
        public static void Main()
        {
            Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

            //ParanoidWorker();
            SampleApplication();
            //SubWorker(true);
            //ReqWorker();

            Console.ReadLine();
        }

        private static void ParanoidWorker()
        {
            var sub = new Common.NetMQ.ParanoidPirateWorker();
            sub.Start();
        }

        private static void SubWorker(bool useNetMQ)
        {
            var workMethod = new Func<CommonRequest, bool>(r =>
            {
                Console.WriteLine("[*] Received from {0} request: {1}", r.ClientId.ToString().Substring(30), r.RequestId);
                return true;
            });

            if (useNetMQ)
            {
                var sub = new Common.NetMQ.SubWithPoller<CommonRequest>("tcp://127.0.0.1:5020", string.Empty, workMethod);
                sub.Start();

                Thread.Sleep(8000);
                sub.Stop();
            }
            else
            {
                var sub = new Common.clrzmq.Sub<CommonRequest>("tcp://127.0.0.1:5020", "ACDC", workMethod);
                sub.Start();
            }
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

            var req = new Common.clrzmq.REQ<CommonRequest, CommonReply>("tcp://127.0.0.1:5000", workMethod);
            req.Start(new CommonReply() { Success = true });
        }

        private static void SampleApplication()
        {
            Console.WriteLine("Start sample application");

            Task.Factory.StartNew(() =>
            {
                var serviceAPI = new DataServiceAPI();
                var service = new DataService(serviceAPI);
            });

            Task.Factory.StartNew(() =>
           {
               var notApi = new NotificationServiceAPI();
               var noti = new NotificationService(notApi);
           });
        }
    }
}