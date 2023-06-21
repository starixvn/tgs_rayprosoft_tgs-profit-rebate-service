using MediatR;
using TGS.Core.Infrastructures.Extensions;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;

namespace Tgs.ProfitRebate.Core.Commands;

public class CreatePlayerAllAgentRebatesHandler : IRequestHandler<CreatePlayerAllAgentRebatesRequest, MessageResponse>
{
    private readonly IPlayerAllAgentRebateFactory _playerAllAgentRebateFactory;
    private readonly IPlayerAllAgentRebateTaskQueue _taskQueue;

    public CreatePlayerAllAgentRebatesHandler(
        IPlayerAllAgentRebateFactory playerAllAgentRebateFactory,
        IPlayerAllAgentRebateTaskQueue taskQueue)
    {
        _playerAllAgentRebateFactory =
            playerAllAgentRebateFactory ?? throw new ArgumentNullException(nameof(playerAllAgentRebateFactory));
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    public async Task<MessageResponse> Handle(CreatePlayerAllAgentRebatesRequest request,
        CancellationToken cancellationToken)
    {
        var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);
        var reportDate = new DateTimeOffset(previousDay.Year, previousDay.Month, previousDay.Day, 0, 0, 0,
            previousDay.Offset);

        if (request.Date.IsNotNullOrEmpty() &&
            DateTimeOffset.TryParse($"{request.Date} 00:00 +08:00", out var result))
        {
            reportDate = new(result.Year, result.Month, result.Day, 0, 0, 0,
                result.Offset);
        }

        var validPlayerCodes = await _playerAllAgentRebateFactory.GetValidAllAgentPlayerCodesAsync(reportDate)
            .ConfigureAwait(false);

        var brandIds = validPlayerCodes.Select(x => x.Item1).Distinct().ToArray();

        var brandAllAgentGameTypeConditions =
            await _playerAllAgentRebateFactory.GetBrandAllAgentGameTypeConditionsAsync(brandIds);

        foreach (var (brandId, playerCode) in validPlayerCodes)
        {
            var conditions = brandAllAgentGameTypeConditions.Where(x => x.BrandId == brandId);
            await _taskQueue.QueuePlayerRebateAsync(new()
            {
                ReportDate = reportDate,
                PlayerCode = playerCode,
                Conditions = conditions
            });
        }

        return new()
        {
            Valid = true
        };
    }
}