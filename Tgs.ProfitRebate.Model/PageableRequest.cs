namespace Tgs.ProfitRebate.Model
{
    public class PageableRequest
    {
        public int Page { get; set; }

        public int Rows { get; set; }

        public SortOrder OrderBy { get; set; }

        public bool IsDesc => OrderBy == SortOrder.Descending;

        public string? SortBy { get; set; }

        public bool ShowAll { get; set; }

        public SortOption GetSortOption() => new(SortBy, IsDesc);
    }
}