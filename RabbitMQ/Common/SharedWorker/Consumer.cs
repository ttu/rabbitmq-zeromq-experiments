using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace Common.SharedWorker
{
    public class Consumer<T>
    {
        private string _hostName;
        private string _queueName;

        public Consumer(string hostName = "localhost", string queueName = "CommonRequest_queue")
        {
            _hostName = "localhost";
            _queueName = queueName;
        }

        public void Start(Func<T, bool> processRequest)
        {
            var factory = new ConnectionFactory() { HostName = _hostName };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(_queueName, true, false, false, null);

                    // Fair dispatch
                    channel.BasicQos(0, 1, false);

                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(_queueName, false, consumer);

                    while (true)
                    {
                        var ea = consumer.Queue.Dequeue() as BasicDeliverEventArgs;

                        try
                        {
                            var body = SerializationMethods.FromByteArray<T>(ea.Body);

                            var success = processRequest(body);

                            // Send acknowledge that received (and processed)
                            if (success)
                                channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception:" + ex.Message);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            // TODO: Stop
        }
    }
}