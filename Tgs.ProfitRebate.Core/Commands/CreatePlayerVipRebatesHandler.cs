using MediatR;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Commands;

public class
    CreatePlayerVipRebatesHandler : IRequestHandler<CreateVipPlayerRebatesRequest,
        MessageResponse>
{
    private readonly IPlayerRebateTaskQueue _taskQueue;

    public CreatePlayerVipRebatesHandler(IPlayerRebateTaskQueue taskQueue)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    public async Task<MessageResponse> Handle(CreateVipPlayerRebatesRequest request,
        CancellationToken cancellationToken)
    {
        await _taskQueue.QueuePlayerRebateAsync(new(request.BrandCode, request.Date, cancellationToken));

        return new()
        {
            Valid = true
        };
    }
}