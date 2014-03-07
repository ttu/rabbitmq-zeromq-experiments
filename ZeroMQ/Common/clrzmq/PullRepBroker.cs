using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZeroMQ;

namespace Common.clrzmq
{
    // Reply
    public class PullRepBroker<TRequest, TResponse>
    {
        private string _clientEndPoint;
        private string _workerEndPoint;

        private Func<TRequest, bool> _clientProcess;
        private Func<TResponse, bool> _workerProcess;

        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        public PullRepBroker(string clientEndpoint, string workerEndpoint, Func<TRequest, bool> clientProcess, Func<TResponse, bool> workerProcess)
        {
            _clientEndPoint = clientEndpoint;
            _workerEndPoint = workerEndpoint;
            _clientProcess = clientProcess;
            _workerProcess = workerProcess;
        }

        public void Start()
        {
            try
            {
                using (var context = ZmqContext.Create())
                {
                    using (ZmqSocket pullSocket = context.CreateSocket(SocketType.PULL),
                                repSocket = context.CreateSocket(SocketType.REP))
                    {
                        pullSocket.Bind(_clientEndPoint);
                        repSocket.Bind(_workerEndPoint);

                        pullSocket.ReceiveReady += pullSocket_ReceiveReady;
                        repSocket.ReceiveReady += repSocket_ReceiveReady;

                        var poller = new Poller(new List<ZmqSocket> { pullSocket, repSocket });

                        while (true)
                        {
                            poller.Poll();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void pullSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            var messageFrame = e.Socket.ReceiveFrame();
            var request = SerializationMethods.FromByteArray<TRequest>(messageFrame.Buffer);

            var success = _clientProcess(request);

            if (success)
                _requests.Add(request);
        }

        private void repSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            var rcvdMsg = e.Socket.ReceiveFrame();
            var response = SerializationMethods.FromByteArray<TResponse>(rcvdMsg.Buffer);

            var success = _workerProcess(response);

            if (success)
            {
                // NOTE: _requests will block other REQs from connecting
                // Should use e.g. some Pirate pattern
                var request = _requests.Take();
                var frame = new Frame(request.ToByteArray());
                e.Socket.SendFrame(frame);
            }
        }
    }
}