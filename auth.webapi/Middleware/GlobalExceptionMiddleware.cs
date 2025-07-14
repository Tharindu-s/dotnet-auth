using System.Net;
using System.Text.Json;
using auth.webapi.Helpers;

namespace auth.webapi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                _logger.LogError(ex, "Application exception occurred");

                context.Response.ContentType = "application/json";

                var statusCode = ExceptionStatusCodeMapper.Map.TryGetValue(ex.GetType(), out var code)
                    ? code
                    : HttpStatusCode.BadRequest;

                context.Response.StatusCode = (int)statusCode;

                var errorResponse = new
                {
                    message = ex.Message,
#if DEBUG
                    detail = ex.StackTrace
#endif
                };

                var json = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                var errorResponse = new
                {
                    message = "An unexpected error occurred.",
#if DEBUG
                    detail = ex.Message
#endif
                };

                var json = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
