using MediatR;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core.Commands
{
    public class CreateAgentDividendsHandler : IRequestHandler<CreateAgentDividendsRequest,
        MessageResponse>
    {
        private readonly IAgentDividendTaskQueue _taskQueue;

        public CreateAgentDividendsHandler(
            IAgentDividendTaskQueue taskQueue)
        {
            _taskQueue = taskQueue;
        }

        public async Task<MessageResponse> Handle(
            CreateAgentDividendsRequest request,
            CancellationToken cancellationToken)
        {
            await _taskQueue.QueueAgentDividendAsync(new AgentDividendQueueModel(request.BrandCode, cancellationToken));

            return new MessageResponse
            {
                Valid = true
            };
        }
    }
}