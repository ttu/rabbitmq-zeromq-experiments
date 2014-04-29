using Common.NetMQ;
using System;

namespace Worker.ExampleApplication
{
    public class NotificationServiceAPI
    {
        private Pull<string> _pull;

        public void Start(Action<string> handleAction)
        {
            _pull = new Pull<string>("tcp://127.0.0.1:5052", handleAction);
            _pull.Start();
        }
    }
}