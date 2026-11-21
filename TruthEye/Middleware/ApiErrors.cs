using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TruthEye.Exceptions;
using TruthEye.Swagger;

namespace TruthEye.Middleware
{
    public static class ApiErrors
    {
        /// exposePlainMessages = true: Exception العادية رسالتها بتتعرض (مرحلة انتقالية للكنترولرات).
        /// false: أي Exception عادية تبقى خطأ غير متوقع → 500 برسالة عامة (الـ middleware).
        public static (int Status, string Message, string Code) Map(Exception ex, bool exposePlainMessages)
        {
            switch (ex)
            {
                case AppException a:
                    return (a.StatusCode, a.Message, a.Code);

                case UnauthorizedAccessException:
                    return (403, "Access denied", "FORBIDDEN");

                case DbUpdateException d when IsUnique(d):
                    return (409, "A record with the same unique value already exists", "CONFLICT");

                case DbUpdateException d when IsForeignKey(d):
                    return (409, "This record is in use and can't be changed", "CONFLICT");

                case DbUpdateException:
                case SqlException:
                    return (500, "Couldn't save your changes. Please try again", "DB_ERROR");

                default:
                    return exposePlainMessages
                        ? (400, ex.Message, "BAD_REQUEST")
                        : (500, "Something went wrong. Please try again", "SERVER_ERROR");
            }
        }

        public static ObjectResult Result(Exception ex, ILogger logger, string action)
        {
            var (status, message, code) = Map(ex, exposePlainMessages: false);
            Log(logger, ex, status, action);
            return new ObjectResult(new ApiErrorResponse { Success = false, Error = message, ErrorCode = code })
            { StatusCode = status };
        }

        public static void Log(ILogger logger, Exception ex, int status, string action)
        {
            if (status >= 500) logger.LogError(ex, "{Action} failed", action);
            else logger.LogWarning("{Action} rejected ({Status}): {Message}", action, status, ex.Message);
        }

        private static bool IsUnique(DbUpdateException d) =>
            d.InnerException is SqlException s && (s.Number == 2601 || s.Number == 2627);

        private static bool IsForeignKey(DbUpdateException d) =>
            d.InnerException is SqlException s && s.Number == 547;
    }
}