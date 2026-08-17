using System.Runtime.Serialization;

namespace JiwaMcpServer.Exceptions
{
    public class RecordNotFoundException : Exception, ISerializable
    {

        public RecordNotFoundException() : base()
        {
            // Add implementation.
        }

        public RecordNotFoundException(string message) : base(message)
        {
            // Add implementation.
        }

        public RecordNotFoundException(string message, Exception inner) : base(message, inner)
        {
            // Add implementation.
        }

        // This constructor is needed for serialization.
        protected RecordNotFoundException(SerializationInfo info, StreamingContext context) : base()
        {
            // Add implementation.
        }
    }
}
