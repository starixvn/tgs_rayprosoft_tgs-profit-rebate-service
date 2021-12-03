namespace Tgs.ProfitRebate.Core.Models
{
    public class PagedArray<T>
    {
        public int TotalCount { get; private set; }

        public T[] Items { get; private set; }

        public PagedArray(int totalCount, T[] items)
        {
            TotalCount = totalCount;
            Items = items;
        }
    }
}
