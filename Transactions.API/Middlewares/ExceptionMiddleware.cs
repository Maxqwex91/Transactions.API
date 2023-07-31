using Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace Transactions.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an exception in the middleware: {ex.Message}");
                HandleException(httpContext, ex);
            }
        }

        public async void HandleException(HttpContext httpContext, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            string exceptionMessage = exception.Message;

            if (exception is BaseException baseException)
            {
                exceptionMessage = baseException.Message;
                statusCode = baseException.StatusCode;
            }
            httpContext.Response.Clear();
            httpContext.Response.Headers.CacheControl = "no-cache,no-store";
            httpContext.Response.Headers.Pragma = "no-cache";
            httpContext.Response.Headers.Expires = "-1";
            httpContext.Response.Headers.ETag = default;

            var result = JsonSerializer.Serialize(exceptionMessage);
            httpContext.Response.StatusCode = (int)statusCode;
            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(result);
        }
    }
}
