using System;

namespace Common.Data.ExampleApplication
{
    public enum RequestType
    { 
        Get,
        Set
    }

    [Serializable]
    public class DataRequest
    {
        public RequestType RequestType { get; set; }

        public int Id { get; set; }

        public object Value { get; set; }
    }
}