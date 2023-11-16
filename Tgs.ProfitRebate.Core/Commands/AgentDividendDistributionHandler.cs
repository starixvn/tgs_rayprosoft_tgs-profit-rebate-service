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

    public AgentDividendDistributionHandler(
        ICashServiceClient cashServiceClient,
        ITgsService<Agent> agentService,
        ITgsService<AgentDividend> agentDividendService)
    {
        _cashServiceClient = cashServiceClient;
        _agentService = agentService;
        _agentDividendService = agentDividendService;
    }

    public async Task Handle(AgentDividendDistributionRequest request,
        CancellationToken cancellationToken)
    {
        var dividendAmounts = await _agentDividendService.GetQueries()
            .Where(x => x.AgentId == request.AgentId && x.SentOn == null)
            .Where(x => x.AgentDividendAmount > 0)
            .ToArrayAsync(cancellationToken);

        var agent = await _agentService.GetQueries().FirstAsync(x => x.Id == request.AgentId, cancellationToken);

        foreach (var dividendAmount in dividendAmounts)
        {
            await _agentDividendService.GetQueries()
                .Where(a => a.Id == dividendAmount.Id)
                .ExecuteUpdateAsync(e => e.SetProperty(s => s.SentOn, DateTime.UtcNow), cancellationToken);

            await _cashServiceClient.AgentDividendAsync(agent.WalletId!.Value,
                dividendAmount.AgentDividendAmount,
                null,
                dividendAmount.Id.ToString(),
                cancellationToken);
        }
    }
}