
namespace TruthEye.Swagger
{
    public class ApiErrorResponse
    {
        /// <summary>
        /// Indicates if the request was successful (always false for errors)
        /// </summary>
        /// <example>false</example>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Error message describing what went wrong
        /// </summary>
        /// <example>User with this email or national ID already exists</example>
        public string Error { get; set; }

        /// <summary>
        /// Optional error code for specific error types
        /// </summary>
        /// <example>VALIDATION_ERROR</example>
        public string ErrorCode { get; set; }
        public Dictionary<string, string[]> ValidationErrors { get; internal set; }
    }
}
