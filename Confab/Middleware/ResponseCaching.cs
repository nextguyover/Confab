namespace Confab.Middleware         //https://stackoverflow.com/a/69754853/9112181
{
    public class CacheResponseMetadata
    {
        // add configuration properties if needed
    }

    public class AddCacheHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public AddCacheHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.GetEndpoint()?.Metadata.GetMetadata<CacheResponseMetadata>() is { } mutateResponseMetadata)
            {
                if (httpContext.Response.HasStarted)
                {
                    throw new InvalidOperationException("Can't mutate response after headers have been sent to client.");
                }
                httpContext.Response.Headers.CacheControl = new[] { "public", "max-age=604800" };
            }
            await _next(httpContext);
        }
    }
}
