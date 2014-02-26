using System;

namespace Common
{
    [Serializable]
    public class CommonReply
    {
        public Guid ClientId { get; set; }

        public int ReplyId { get; set; }

        public bool Success { get; set; }
    }
}