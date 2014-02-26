using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Common
{
    public class Pull<T>
    {
         private List<string> _bindEndPoints;
        private Func<T, bool> _process;

        public Pull(string endpoint, Func<T, bool> process)
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
                    using (var socket = context.CreateSocket(SocketType.PULL))
                    {
                        foreach (var bindEndPoint in _bindEndPoints)
                            socket.Bind(bindEndPoint);

                        while (true)
                        {
                            var messageFrame = socket.ReceiveFrame();
                            var request = SerializationMethods.FromByteArray<T>(messageFrame.Buffer);
                            _process(request);
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
