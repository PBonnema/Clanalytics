using System;

namespace Ingestion.Exceptions
{
    [Serializable]
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException() { }
        public UserNotFoundException(string message) : base(message) { }
        public UserNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected UserNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
