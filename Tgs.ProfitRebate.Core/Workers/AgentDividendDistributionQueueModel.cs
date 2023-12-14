namespace Tgs.ProfitRebate.Core.Workers;

public record AgentDividendDistributionQueueModel(Guid AgentId, decimal DividendAmount, int ExternalId, CancellationToken CancellationToken);