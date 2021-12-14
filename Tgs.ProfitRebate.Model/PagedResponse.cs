namespace Tgs.ProfitRebate.Model
{
    public class PagedResponse<T> : MessageResponse
    {
        public int TotalCount { get; set; }
        public T[] Items { get; set; }

        public PagedResponse()
        {
        }

        public PagedResponse(int count, T[] items)
        {
            TotalCount = count;
            Items = items;
        }
    }
}