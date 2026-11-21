using TruthEye.Swagger;

namespace TruthEye.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
            {
                // العميل قفل الاتصال، مش خطأ
            }
            catch (Exception ex)
            {
                if (ctx.Response.HasStarted)
                {
                    _logger.LogError(ex, "Error after the response started: {Path}", ctx.Request.Path);
                    throw;
                }

                var (status, message, code) = ApiErrors.Map(ex, exposePlainMessages: false);
                ApiErrors.Log(_logger, ex, status, $"{ctx.Request.Method} {ctx.Request.Path}");

                ctx.Response.Clear();
                ctx.Response.StatusCode = status;
                await ctx.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Success = false,
                    Error = message,
                    ErrorCode = code
                });
            }
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app) =>
            app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}