namespace TruthEye.Exceptions
{
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }
        public string Code { get; }

        protected AppException(int statusCode, string code, string message) : base(message)
        {
            StatusCode = statusCode;
            Code = code;
        }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message) : base(400, "BAD_REQUEST", message) { }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message = "Access denied") : base(403, "FORBIDDEN", message) { }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message = "Not found") : base(404, "NOT_FOUND", message) { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(409, "CONFLICT", message) { }
    }
}