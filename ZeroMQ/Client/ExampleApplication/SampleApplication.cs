using System;
using System.Threading.Tasks;

namespace Client.ExampleApplication
{
    public interface IDataService
    {
        Task<object> GetData(int id);

        Task<object> SetData(int id, object data);
    }

    public interface INotificationService
    {
        Task Send(string value);
    }

    public class SampleApplication
    {
        private IDataService _service;
        private INotificationService _notification;

        public SampleApplication(IDataService service, INotificationService notification)
        {
            _service = service;
            _notification = notification;
        }

        public async Task GetData()
        {
            Console.WriteLine("Data request: 1");

            var value = await _service.GetData(1);
            Console.WriteLine("Got Data: {0}", value);
        }

        public async Task SetData()
        {
            Console.WriteLine("Set data: 6");

            var value = await _service.SetData(1, 6);

            Console.WriteLine("Data set from service: {0}", value);
        }

        public void PublishNotification(string test)
        {
            Console.WriteLine("Sending: {0}", test);
            _notification.Send(test);
        }
    }
}