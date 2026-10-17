namespace TruthEye.Swagger
{
    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
