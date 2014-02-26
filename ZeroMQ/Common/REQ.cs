using System;
using System.Collections.Generic;
using ZeroMQ;

namespace Common
{
    // Request
    public class REQ<T, U>
    {
        private List<string> _bindEndPoints;
        private Func<T, U> _process;

        public REQ(string endPoint, Func<T, U> process)
        {
            _bindEndPoints = new List<string> { endPoint };
            _process = process;
        }

        public void Start(U startResponse)
        {
            try
            {
                using (var context = ZmqContext.Create())
                {
                    using (var socket = context.CreateSocket(SocketType.REQ))
                    {
                        foreach (var connectEndpoint in _bindEndPoints)
                            socket.Connect(connectEndpoint);

                        var nextResponse = startResponse;

                        while (true)
                        {
                            var responseFrame = new Frame(nextResponse.ToByteArray());
                            socket.SendFrame(responseFrame);

                            var messageFrame = socket.ReceiveFrame();
                            var request = SerializationMethods.FromByteArray<T>(messageFrame.Buffer);

                            var reply = _process(request);

                            nextResponse = reply;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}