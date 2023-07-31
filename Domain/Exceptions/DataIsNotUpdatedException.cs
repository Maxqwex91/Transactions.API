using System.Net;

namespace Domain.Exceptions
{
    public class DataIsNotUpdatedException : BaseException
    {
        public DataIsNotUpdatedException(string message = "Server could not insert or update data") : base(message)
        {
            StatusCode = HttpStatusCode.NotFound;
        }

        public DataIsNotUpdatedException(Exception innerException, string message = "Server could not insert or update data") : base(innerException, message)
        {
        }
    }
}