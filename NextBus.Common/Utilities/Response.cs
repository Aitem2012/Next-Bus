using System.Collections.Generic;

namespace NextBus.Common.Utilities
{
    public class Response<T>
    {
        public string Message { get; set; }
        public Dictionary<string, string> Errs { get; set; }
        public T Data { get; set; }
    }
}
