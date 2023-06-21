namespace Tgs.ProfitRebate.Core.ViewModel;

public class SortOption
{
    public string? PropertyName { get; set; }

    public bool IsDescending { get; set; }

    public SortOption(string? propertyName, bool isDescending)
    {
        IsDescending = isDescending;
        PropertyName = propertyName;
    }
}