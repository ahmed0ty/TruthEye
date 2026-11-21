namespace TruthEye.DTOs
{
    public class PageQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }
        public string? Status { get; set; }      // active | inactive | (فاضي = الكل)
        public int? DepartmentId { get; set; }
        public int? Year { get; set; }
        public string? Sort { get; set; }
        public string? Dir { get; set; }         // asc | desc

        public int SafePage => Math.Max(1, Page);
        public int SafeSize => Math.Clamp(PageSize, 1, 100);
        public bool Desc => string.Equals(Dir, "desc", StringComparison.OrdinalIgnoreCase);
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}