using MediatR;
using Microsoft.EntityFrameworkCore;
using Tgs.ProfitRebate.Repository.Models;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Commands
{
    public class CreateAgentDividendsHandler : IRequestHandler<CreateAgentDividendsRequest,
        MessageResponse>
    {
        private readonly IAgentDividendFactory _agentDividendFactory;
        private readonly ITgsService<AgentDividend> _agentDividendService;

        public CreateAgentDividendsHandler(
            ITgsService<AgentDividend> agentDividendService,
            IAgentDividendFactory agentDividendFactory)
        {
            _agentDividendService = agentDividendService;
            _agentDividendFactory = agentDividendFactory;
        }

        public async Task<MessageResponse> Handle(
            CreateAgentDividendsRequest request,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));
            var queryDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, new(8, 0, 0));

            await SendAgentDividend(request.BrandCode, queryDate, cancellationToken);

            return new MessageResponse
            {
                Valid = true
            };
        }

        private async Task SendAgentDividend(
            string? brandCode, DateTimeOffset queryDate,
            CancellationToken cancellationToken)
        {
            await foreach (var brands in _agentDividendFactory.GetSettlementIntervalBrands(brandCode, queryDate))
            {
                var createModels = new List<CreateAgentDividendModel>();
                var createAutoSendModels = new List<CreateAgentDividendModel>();
                var updateAgentDividends = new List<AgentDividend>();

                var maxNumber = await _agentDividendService.GetQueries()
                    .MaxAsync(x => (int?)x.Id, cancellationToken) ?? 0;

                foreach (var brand in brands)
                {
                    if (!await _agentDividendService.GetQueries()
                            .AnyAsync(
                                x => x.BrandId == brand.Id && brand.CurrentSettlementFrom == x.From &&
                                    brand.CurrentSettlementTo == x.To,
                                cancellationToken))
                    {
                        var (autoSendDividends, manuelOrNegativeDividends, agentDividends, backMaxNumber) =
                            await _agentDividendFactory.GetAgentDividends(brand, maxNumber);

                        maxNumber = backMaxNumber;

                        createAutoSendModels.AddRange(autoSendDividends);
                        createModels.AddRange(manuelOrNegativeDividends);
                        updateAgentDividends.AddRange(agentDividends);
                    }

                    var (nextFrom, nextTo) =
                        _agentDividendFactory.GetInterval(brand.SettlementIntervalType,
                            brand.NextSettlementTo.ToOffset(new(8, 0, 0)).AddDays(1),
                            brand.SettlementDay);

                    brand.PreviousSettlementFrom = brand.CurrentSettlementFrom;
                    brand.PreviousSettlementTo = brand.CurrentSettlementTo;
                    brand.CurrentSettlementFrom = brand.NextSettlementFrom;
                    brand.CurrentSettlementTo = brand.NextSettlementTo;
                    brand.NextSettlementFrom = nextFrom;
                    brand.NextSettlementTo = nextTo;
                }

                await _agentDividendFactory.SaveAgentDividendData(brands,
                    createModels,
                    createAutoSendModels,
                    updateAgentDividends);
            }
        }
    }
}