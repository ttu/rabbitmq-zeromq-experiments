using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZeroMQ;

namespace Common
{
    public class Push<TRequest>
    {
        private List<string> _bindEndPoints;
        private BlockingCollection<TRequest> _requests = new BlockingCollection<TRequest>();

        public Push(string endpoint)
        {
            _bindEndPoints = new List<string> { endpoint };
        }

        public void Start()
        {
            try
            {
                using (var context = ZmqContext.Create())
                {
                    using (var socket = context.CreateSocket(SocketType.PUSH))
                    {
                        foreach (var bindEndPoint in _bindEndPoints)
                            socket.Connect(bindEndPoint);

                        while (true)
                        {
                            var message = _requests.Take();
                            var frame = new Frame(message.ToByteArray());
                            socket.SendFrame(frame);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddMessage(TRequest msg)
        {
            _requests.Add(msg);
        }
    }
}