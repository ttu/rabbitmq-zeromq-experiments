using Common.Data.ExampleApplication;
using Common.NetMQ;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Worker.ExampleApplication
{

    public class DataService
    {
        private DataServiceAPI _api;

        private ConcurrentDictionary<int, object> _storage = new ConcurrentDictionary<int, object>();

        public DataService(DataServiceAPI api)
        {
            _api = api;
            _api.Start(HandleDataRequest);
        }

        public DataReply HandleDataRequest(DataRequest request)
        {
            if (request.RequestType == RequestType.Get)
            {
                Console.WriteLine("Fetching data from db");
                Thread.Sleep(4000); // Hard work, get stuff from db etc.

                if (!_storage.ContainsKey(request.Id))
                    _storage[request.Id] = 0;

                var value = _storage[request.Id];

                return new DataReply { Value = value };
            }
            else
            {
                Console.WriteLine("Set data from db");
                Thread.Sleep(2000); // Hard work, get stuff from db etc.

                _storage[request.Id] = request.Value;

                return new DataReply { Value = _storage[request.Id] };
            }
        }
    }
}