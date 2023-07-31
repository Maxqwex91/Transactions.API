using System.Net;

namespace Domain.Exceptions
{
    public abstract class BaseException : Exception
    {
        public virtual HttpStatusCode StatusCode { get; set; }

        public BaseException(string message) : base(message)
        {
        }

        public BaseException(Exception innerException, string message) : base(message, innerException)
        {
        }
    }
}