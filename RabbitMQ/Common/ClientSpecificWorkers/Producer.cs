using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Common.ClientSpecificWorkers
{
    public class Producer<TRequest, TResponse> : IDisposable
        where TRequest : CommonRequest // Actually no point in using generics in these examples
    {
        private string _hostName;
        private string _responseQueueName;
        private string _exchangeName;

        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _model;
        private QueueingBasicConsumer _consumer;

        private Action<TRequest> _processSent;
        private Func<TResponse, bool> _processRequest;

        private Task _sender;
        private Task _receiver;
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        private bool _disposed;

        public Producer(Action<TRequest> processSent, Func<TResponse, bool> processRequest, string hostName = "localhost")
        {
            _processSent = processSent;
            _processRequest = processRequest;

            _hostName = hostName;
            _exchangeName = "CustomerExchange";

            _connectionFactory = new ConnectionFactory() { HostName = _hostName };
            _connection = _connectionFactory.CreateConnection();
            _model = _connection.CreateModel();

            // If excange doesn't exist, create it
            _model.ExchangeDeclare(_exchangeName, ExchangeType.Direct, true, false, null);

            _responseQueueName = _model.QueueDeclare().QueueName; // Declare a random queue for the responses

            Console.WriteLine("[P] Created response queue: " + _responseQueueName);

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
                try
                {
                    var message = _requests.Take(token);

                    //Setup properties
                    var properties = _model.CreateBasicProperties();
                    properties.ReplyTo = _responseQueueName;
                    properties.CorrelationId = message.RequestId.ToString();
                    properties.SetPersistent(true);

                    var body = message.ToByteArray();

                    _model.BasicPublish(_exchangeName, message.ClientId.ToString(), properties, message.ToByteArray());

                    _processSent(message);
                }
                catch (OperationCanceledException)
                {
                    continue;
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
                catch (EndOfStreamException ex)
                {
                    // This comes when connection is closed (some better way to detect this?)
                    if (ex.Message == "SharedQueue closed")
                        return;
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