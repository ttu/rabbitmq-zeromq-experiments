using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Common.ScatterGather
{
    public class Producer<TRequest, TResponse> : IDisposable
    {
        private string _hostName;
        private string _queueName;
        private string _responseQueueName;

        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private QueueingBasicConsumer _consumer;

        private Func<TResponse, bool> _processRequest;

        private Task _sender;
        private Task _receiver;
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        private bool _disposed;

        public Producer(Func<TResponse, bool> processRequest, string hostName = "localhost", string queueName = "ScatterGather_WorkQueue")
        {
            _processRequest = processRequest;
            _hostName = hostName;
            _queueName = queueName;

            _connectionFactory = new ConnectionFactory() { HostName = _hostName };
            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();
            _model.QueueDeclare(_queueName, true, false, false, null);

            _responseQueueName = _model.QueueDeclare().QueueName; // Declare a random queue for the responses
            _consumer = new QueueingBasicConsumer(_model);
            _model.BasicConsume(_responseQueueName, true, _consumer);
        }

        ~Producer()
        {
            Dispose(false);
        }

        public bool IsRunning { get; private set; }

        public void Start()
        {
            IsRunning = true;
            _sender = Task.Factory.StartNew((t) => Send(t), _cancellationSource.Token, TaskCreationOptions.LongRunning);
            _receiver = Task.Factory.StartNew((t) => Receive(t), _cancellationSource.Token, TaskCreationOptions.LongRunning);
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

        public void Send(TRequest message)
        {
            _requests.Add(message);
        }

        private void Send(object t)
        {
            var token = (CancellationToken)t;

            while (token.IsCancellationRequested == false)
            {
                var message = _requests.Take(token);
                var correlationToken = Guid.NewGuid().ToString(); //TODO: Get from message

                try
                {
                    //Setup properties
                    var properties = _model.CreateBasicProperties();
                    properties.ReplyTo = _responseQueueName;
                    properties.CorrelationId = correlationToken;
                    properties.SetPersistent(true);

                    var body = message.ToByteArray();

                    _model.BasicPublish("", _queueName, properties, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception:" + ex.Message);
                }
            }
        }

        private void Receive(object t)
        {
            var token = (CancellationToken)t;

            while (token.IsCancellationRequested == false)
            {
                try
                {
                    var ea = _consumer.Queue.Dequeue();

                    var body = SerializationMethods.FromByteArray<TResponse>(ea.Body);

                    var success = _processRequest(body);
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