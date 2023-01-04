namespace Tgs.ProfitRebate.Core.ViewModel;

public class PagedArray<T>
{
	public int TotalCount { get; }

	public T[] Items { get; }

	public PagedArray(int totalCount, T[] items)
	{
		TotalCount = totalCount;
		Items = items;
	}
}
