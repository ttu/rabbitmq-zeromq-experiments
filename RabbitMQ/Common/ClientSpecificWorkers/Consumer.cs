using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.ClientSpecificWorkers
{
    public class Consumer<TRequest, TResponse> : IDisposable
    {
        private string _hostName;
        private string _exchangeName;
        private Guid _clientId;
        private string _queueName;

        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private Subscription _subscription;

        private Func<TRequest, TResponse> _processRequest;

        private Task _listener;
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        private bool _disposed;

        public Consumer(Func<TRequest, TResponse> processRequest, Guid clientId, string hostName = "localhost")
        {
            _processRequest = processRequest;
            _hostName = hostName;
            _exchangeName = "CustomerExchange";
            _clientId = clientId;

            _connectionFactory = new ConnectionFactory() { HostName = _hostName };
            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();

            // Fair dispatch
            _model.BasicQos(0, 1, false);

            // If excange doesn't exist, create it
            _model.ExchangeDeclare(_exchangeName, ExchangeType.Direct, true, false, null);

            // TODO: If want to have multiple consumers for queues, this should have some defined name
            _queueName = _model.QueueDeclare().QueueName;
            _model.QueueBind(_queueName, _exchangeName, _clientId.ToString());

            _subscription = new Subscription(_model, _queueName, false);
        }

        ~Consumer()
        {
            Dispose(false);
        }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
            _listener = Task.Factory.StartNew((t) => Run(t), _cancellationSource.Token, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            IsRunning = false;
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Run(object t)
        {
            var token = (CancellationToken)t;

            while (token.IsCancellationRequested == false)
            {
                try
                {
                    var deliveryArgs = _subscription.Next();

                    if (deliveryArgs == null) // Connection is closed
                        continue;

                    var body = SerializationMethods.FromByteArray<TRequest>(deliveryArgs.Body);

                    TResponse response = _processRequest(body);

                    // Send Response
                    var replyProperties = _model.CreateBasicProperties();
                    replyProperties.CorrelationId = deliveryArgs.BasicProperties.CorrelationId;
                    _model.BasicPublish("", deliveryArgs.BasicProperties.ReplyTo, replyProperties, response.ToByteArray());

                    // Send acknowledge that received (and processed)
                    _model.BasicAck(deliveryArgs.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception:" + ex.Message);
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _cancellationSource.Cancel();
                _cancellationSource.Dispose();

                if (_model != null)
                {
                    if (_model.IsOpen)
                        _model.Abort();
                    _model.Dispose();
                }

                if (_connection != null)
                {
                    if (_connection.IsOpen)
                        _connection.Close();
                    _connection.Dispose();
                }
            }

            _disposed = true;
        }
    }
}