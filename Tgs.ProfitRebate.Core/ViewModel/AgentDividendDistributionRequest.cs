using MediatR;

namespace Tgs.ProfitRebate.Core.ViewModel;

public record AgentDividendDistributionRequest(Guid AgentId) : IRequest;