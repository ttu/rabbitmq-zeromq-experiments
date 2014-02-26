using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZeroMQ;

namespace Common
{
    // Reply
    public class REP<TRequest, TResponse>
    {
        private List<string> _bindEndPoints;
        private Func<TResponse, bool> _process;

        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        public REP(string endpoint, Func<TResponse, bool> process)
        {
            _bindEndPoints = new List<string> { endpoint };
            _process = process;
        }

        public void Start()
        {
            try
            {
                using (var context = ZmqContext.Create())
                {
                    using (var socket = context.CreateSocket(SocketType.REP))
                    {
                        foreach (var bindEndPoint in _bindEndPoints)
                            socket.Bind(bindEndPoint);

                        while (true)
                        {
                            // Wait for a message
                            var rcvdMsg = socket.ReceiveFrame();
                            var response = SerializationMethods.FromByteArray<TResponse>(rcvdMsg.Buffer);

                            var ok = _process(response);

                            if (ok)
                            {
                                // NOTE: _requests will block other REQs from connecting
                                // Should use e.g. some Pirate pattern
                                var request = _requests.Take();
                                var frame = new Frame(request.ToByteArray());
                                socket.SendFrame(frame);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddTask(TRequest request)
        {
            _requests.Add(request);
        }
    }
}