using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Repository.Models;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Commands
{
    public class CreateAgentDividendsHandler : IRequestHandler<CreateAgentDividendsRequest,
        MessageResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAgentDividendTaskQueue _taskQueue;

        public CreateAgentDividendsHandler(
            IAgentDividendTaskQueue taskQueue,
            IServiceProvider serviceProvider)
        {
            _taskQueue = taskQueue;
            _serviceProvider = serviceProvider;
        }

        public async Task<MessageResponse> Handle(
            CreateAgentDividendsRequest request,
            CancellationToken cancellationToken)
        {
            await _taskQueue.QueueBackgroundWorkItemAsync((SendAgentDividend,
                new AgentDividendQueueModel(request.BrandCode, cancellationToken)));

            return new MessageResponse
            {
                Valid = true
            };
        }

        private async ValueTask SendAgentDividend(AgentDividendQueueModel request)
        {
            using var scope = _serviceProvider.CreateScope();

            var agentDividendFactory = scope.ServiceProvider.GetRequiredService<IAgentDividendFactory>();
            var agentDividendService = scope.ServiceProvider.GetRequiredService<ITgsService<AgentDividend>>();

            var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));
            var queryDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, new(8, 0, 0));

            await foreach (var brands in
                           agentDividendFactory.GetSettlementIntervalBrands(request.BrandCode, queryDate))
            {
                var createModels = new List<CreateAgentDividendModel>();
                var createAutoSendModels = new List<CreateAgentDividendModel>();
                var updateAgentDividends = new List<AgentDividend>();

                var maxNumber = await agentDividendService.GetQueries()
                    .MaxAsync(x => (int?)x.Id, request.CancellationToken) ?? 0;

                foreach (var brand in brands)
                {
                    if (!await agentDividendService.GetQueries()
                            .AnyAsync(
                                x => x.BrandId == brand.Id && brand.CurrentSettlementFrom == x.From &&
                                    brand.CurrentSettlementTo == x.To,
                                request.CancellationToken))
                    {
                        var (autoSendDividends, manuelOrNegativeDividends, agentDividends, backMaxNumber) =
                            await agentDividendFactory.GetAgentDividends(brand, maxNumber);

                        maxNumber = backMaxNumber;

                        createAutoSendModels.AddRange(autoSendDividends);
                        createModels.AddRange(manuelOrNegativeDividends);
                        updateAgentDividends.AddRange(agentDividends);
                    }

                    var (nextFrom, nextTo) =
                        agentDividendFactory.GetInterval(brand.SettlementIntervalType,
                            brand.NextSettlementTo.ToOffset(new(8, 0, 0)),
                            brand.SettlementDay);

                    brand.PreviousSettlementFrom = brand.CurrentSettlementFrom;
                    brand.PreviousSettlementTo = brand.CurrentSettlementTo;
                    brand.CurrentSettlementFrom = brand.NextSettlementFrom;
                    brand.CurrentSettlementTo = brand.NextSettlementTo;
                    brand.NextSettlementFrom = nextFrom.ToUniversalTime();
                    brand.NextSettlementTo = nextTo.ToUniversalTime();
                }

                await agentDividendFactory.SaveAgentDividendData(brands,
                    createModels,
                    createAutoSendModels,
                    updateAgentDividends);
            }
        }
    }
}