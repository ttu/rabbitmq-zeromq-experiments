using Common.Data.ExampleApplication;
using Common.NetMQ;
using System;

namespace Worker.ExampleApplication
{
    public class DataServiceAPI
    {
        private REP<DataRequest, DataReply> _req;

        public void Start(Func<DataRequest, DataReply> handleAction)
        {
            _req = new REP<DataRequest, DataReply>("tcp://127.0.0.1:5050", handleAction);
            _req.Start();
        }
    }
}