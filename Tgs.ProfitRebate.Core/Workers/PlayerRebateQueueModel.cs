namespace Tgs.ProfitRebate.Core.Workers
{
    public record PlayerRebateQueueModel(string BrandCode, string? Date, CancellationToken CancellationToken);
}