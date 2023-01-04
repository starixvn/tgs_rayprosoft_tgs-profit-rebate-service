using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Core.Workers;

public record PlayerAllAgentRebateQueueModel
{
    public string ParentPlayerCode { get; init; } = default!;

    public DateTimeOffset ReportDate { get; init; }

    public IEnumerable<BrandAllAgentGameTypeConditionModel> Conditions { get; init; } = default!;
}