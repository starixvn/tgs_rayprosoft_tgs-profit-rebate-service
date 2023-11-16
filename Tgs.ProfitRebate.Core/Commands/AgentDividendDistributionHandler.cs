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
            .Where(x => x.AgentDividendAmount > 0)
            .ToArrayAsync(cancellationToken);

        var agent = await _agentService.GetQueries().FirstAsync(x => x.Id == request.AgentId, cancellationToken);

        foreach (var dividendAmount in dividendAmounts)
        {
            await _agentDividendFactory.SetSentOnAsync(dividendAmount.Id, cancellationToken);

            await _cashServiceClient.AgentDividendAsync(agent.WalletId!.Value,
                dividendAmount.AgentDividendAmount,
                null,
                dividendAmount.Id.ToString(),
                cancellationToken);
        }
    }
}