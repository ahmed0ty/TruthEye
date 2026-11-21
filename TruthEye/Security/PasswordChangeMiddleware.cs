using TruthEye.Swagger;

namespace TruthEye.Security
{
    public sealed record UserState(bool Active, bool MustChange);

    public class PasswordChangeMiddleware
    {
        private static readonly string[] Allowed =
        {
            "/api/auth/change-password", "/api/auth/profile",
            "/api/auth/logout", "/api/auth/refresh", "/health"
        };

        private readonly RequestDelegate _next;
        public PasswordChangeMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext ctx)
        {
            if (ctx.User.HasClaim("must_change_password", "1")
                && !Allowed.Any(p => ctx.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            {
                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Success = false,
                    Error = "You must change your password before continuing",
                    ErrorCode = "PASSWORD_CHANGE_REQUIRED"
                });
                return;
            }
            await _next(ctx);
        }
    }
}