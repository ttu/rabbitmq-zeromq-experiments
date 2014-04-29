using NetMQ;
using System;

namespace Common.NetMQ
{
    public class PubWithPoller<TRequest>
    {
        private NetMQContext _zmqContext;
        private NetMQSocket _zmqSocket;
        private Poller _poller;

        private string _endPoint = string.Empty;
        private string _subsPrefix;

        private Func<TRequest, bool> _action;
    }
}