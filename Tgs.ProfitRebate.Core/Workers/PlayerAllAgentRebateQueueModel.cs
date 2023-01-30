using Tgs.ProfitRebate.Core.ViewModel;

namespace Tgs.ProfitRebate.Core.Workers;

public record PlayerAllAgentRebateQueueModel
{
    public string PlayerCode { get; init; } = default!;

    public DateTimeOffset ReportDate { get; init; }

    public IEnumerable<BrandAllAgentGameTypeConditionModel> Conditions { get; init; } = default!;
}