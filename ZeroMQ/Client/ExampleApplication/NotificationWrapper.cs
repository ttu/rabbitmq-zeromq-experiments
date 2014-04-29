using Common.NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.ExampleApplication
{
    public class NotificationWrapper : INotificationService
    {
        private Push<string> _push;

        public NotificationWrapper()
        {
            _push = new Push<string>("tcp://127.0.0.1:5052");
        }

        public async Task Send(string value)
        {
            var toSend = value;

            await Task.Factory.StartNew(() =>
            {
                _push.Send(toSend);
            });
        }
    }
}
