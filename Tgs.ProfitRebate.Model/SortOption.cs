namespace Tgs.ProfitRebate.Model
{
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
}