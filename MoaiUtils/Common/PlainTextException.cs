using System;

namespace MoaiUtils.Common {
    /// <summary>
    /// Simple exception whose message is self-explanatory.
    /// </summary>
    public class PlainTextException : Exception {
        public PlainTextException(string message) : base(message) { }
        
        public PlainTextException(string messageFormat, params object[] args)
            : this(string.Format(messageFormat, args)) { }
    }
}