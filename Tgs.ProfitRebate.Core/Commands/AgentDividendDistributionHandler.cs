using MediatR;
using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.CashService.Client;

namespace Tgs.ProfitRebate.Core.Commands;

public class
    AgentDividendDistributionHandler : IRequestHandler<AgentDividendDistributionRequest>
{
    private readonly ICashServiceClient _cashServiceClient;
    private readonly ITgsService<Agent> _agentService;
    private readonly ITgsService<AgentDividend> _agentDividendService;
    private readonly IAgentDividendFactory _agentDividendFactory;

    public AgentDividendDistributionHandler(
        ICashServiceClient cashServiceClient,
        ITgsService<Agent> agentService,
        ITgsService<AgentDividend> agentDividendService,
        IAgentDividendFactory agentDividendFactory)
    {
        _cashServiceClient = cashServiceClient;
        _agentService = agentService;
        _agentDividendService = agentDividendService;
        _agentDividendFactory = agentDividendFactory;
    }

    public async Task Handle(AgentDividendDistributionRequest request,
        CancellationToken cancellationToken)
    {
        var dividendAmounts = await _agentDividendService.GetQueries()
            .Where(x => x.AgentId == request.AgentId && x.SentOn == null)
            .ToArrayAsync(cancellationToken);

        var total = dividendAmounts.Sum(x => x.AgentDividendAmount);

        if (total > 0)
        {
            var agent = await _agentService.GetQueries().FirstAsync(x => x.Id == request.AgentId, cancellationToken);

            await _agentDividendFactory.SetSentOnAsync(dividendAmounts.Select(x => x.Id), cancellationToken);

            await _cashServiceClient.AgentDividendAsync(agent.WalletId!.Value,
                total,
                null,
                dividendAmounts.Where(x => x.AgentDividendAmount > 0).Max(x => x.Id).ToString(),
                cancellationToken);
        }
    }
}