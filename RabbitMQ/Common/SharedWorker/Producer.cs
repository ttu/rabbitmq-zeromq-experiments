using Common;
using RabbitMQ.Client;
using System.Collections.Generic;

namespace Common.SharedWorker
{
    public class Producer<T>
    {
        private string _hostName;
        private string _queueName;

        public Producer(string hostName = "localhost", string queueName = "CommonRequest_queue")
        {
            _hostName = "localhost";
            _queueName = queueName;
        }

        public void Publish(T message)
        {
            Publish(new List<T> { message });
        }

        public void Publish(List<T> messages)
        {
            var factory = new ConnectionFactory() { HostName = _hostName };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    // TODO: Should declarations for queues be in some common place?
                    channel.QueueDeclare(_queueName, true, false, false, null);

                    foreach (var message in messages)
                    {
                        var body = message.ToByteArray();

                        var properties = channel.CreateBasicProperties();
                        properties.SetPersistent(true);

                        channel.BasicPublish("", _queueName, properties, body);
                    }
                }
            }
        }
    }
}