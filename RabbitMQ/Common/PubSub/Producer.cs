using RabbitMQ.Client;
using System.Collections.Generic;

namespace Common.PubSub
{
    public class Producer
    {
        private string _hostName;
        private string _exchangeName;

        public Producer(string hostName = "localhost", string exchangeName = "Test_Exch")
        {
            _hostName = "localhost";
            _exchangeName = exchangeName;
        }

        public void Publish(CommonRequest message)
        {
            Publish(new List<CommonRequest> { message });
        }

        public void Publish(List<CommonRequest> messages)
        {
            var factory = new ConnectionFactory() { HostName = _hostName };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(_exchangeName, "fanout");

                    foreach (var message in messages)
                    {
                        var body = message.ToByteArray();

                        channel.BasicPublish(_exchangeName, "", null, body);
                    }
                }
            }
        }
    }
}