using System;

namespace Common.Data.ExampleApplication
{
    [Serializable]
    public class DataReply
    {
        public int Id { get; set; }

        public object Value { get; set; }
    }
}