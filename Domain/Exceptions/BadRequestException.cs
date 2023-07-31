using System.Net;

namespace Domain.Exceptions
{
    public class BadRequestException : BaseException
    {
        public BadRequestException(string message = "Bad Request")
            : base(message)
        {
            StatusCode = HttpStatusCode.BadRequest;
        }

        public BadRequestException(Exception innerException, string message = "Bad Request")
            : base(innerException, message)
        {
            StatusCode = HttpStatusCode.BadRequest;
        }
    }
}
