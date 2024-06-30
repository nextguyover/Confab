using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Confab.Middleware
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        private static ILogger _logger;
        public static ILogger logger { set { _logger = value; } }

        public ApiLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.GetEndpoint() != null)
            {
                _logger.LogTrace(httpContext.GetEndpoint().ToString());
            }
            await _next(httpContext);
        }
    }
}
