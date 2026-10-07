namespace AcxiomCRM.Models
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        
        // Search/Filter preservation
        public string? SearchString { get; set; }
        public string? StatusFilter { get; set; }
        public string? CompanyFilter { get; set; }
        public string? AssignedToFilter { get; set; }
    }
}
