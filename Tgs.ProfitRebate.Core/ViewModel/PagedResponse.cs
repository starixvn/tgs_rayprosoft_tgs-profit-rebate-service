using System;

namespace Tgs.ProfitRebate.ViewModel;

public class PagedResponse<T> : MessageResponse
{
	public int TotalCount { get; set; }

	public T[] Items { get; set; }

	public PagedResponse()
	{
		Items = Array.Empty<T>();
	}

	public PagedResponse(int count, T[] items)
	{
		TotalCount = count;
		Items = items;
	}
}
