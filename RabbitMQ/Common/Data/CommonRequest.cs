using System;

namespace Common
{
    [Serializable]
    public class CommonRequest
    {
        public Guid ClientId { get; set; }

        public int RequestId { get; set; }

        public string Message { get; set; }

        public int Duration { get; set; }
    }
}