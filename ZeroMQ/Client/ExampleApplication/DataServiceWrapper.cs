using Common.Data.ExampleApplication;
using Common.NetMQ;
using System.Threading.Tasks;

namespace Client.ExampleApplication
{
    public class DataServiceWrapper : IDataService
    {
        private REQ<DataRequest, DataReply> _req;

        public DataServiceWrapper()
        {
            _req = new REQ<DataRequest, DataReply>("tcp://127.0.0.1:5050");
        }

        public async Task<object> GetData(int id)
        {
            var request = new DataRequest() { RequestType = RequestType.Get, Id = id };

            return await Task.Factory.StartNew<object>(() =>
            {
                var response = _req.SendRequest(request);

                return response.Value;
            });
        }

        public async Task<object> SetData(int id, object value)
        {
            var request = new DataRequest() { RequestType = RequestType.Set, Id = id, Value = value };

            return await Task.Factory.StartNew<object>(() =>
            {
                var response = _req.SendRequest(request);

                return response.Value;
            });
        }
    }
}