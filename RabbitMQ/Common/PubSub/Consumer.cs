using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Common.PubSub
{
    public class Consumer
    {
        private string _hostName;
        private string _exchangeName;

        public Consumer(string hostName = "localhost", string exchangeName = "Test_Exch")
        {
            _hostName = "localhost";
            _exchangeName = exchangeName;
        }

        public void Start(Func<CommonRequest, bool> processRequest)
        {
            var factory = new ConnectionFactory() { HostName = _hostName };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    // Fanout just broadcasts messages
                    channel.ExchangeDeclare(_exchangeName, "fanout");

                    // This will create an own queue to RabbitMQ server
                    var queueName = channel.QueueDeclare();

                    channel.QueueBind(queueName, _exchangeName, "");

                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(queueName, true, consumer);

                    while (true)
                    {
                        var ea = consumer.Queue.Dequeue() as BasicDeliverEventArgs;

                        try
                        {
                            var body = SerializationMethods.FromByteArray<CommonRequest>(ea.Body);

                            var success = processRequest(body);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception:" + ex.Message);
                        }
                    }
                }
            }
        }
    }
}