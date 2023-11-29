using System.Threading.Channels;

namespace Tgs.ProfitRebate.Core.Workers;

public interface IAgentDividendDistributionTaskQueue
{
    ValueTask QueueAgentDividendDistributionAsync(
        AgentDividendDistributionQueueModel model);

    ValueTask<AgentDividendDistributionQueueModel> DequeueAsync(
        CancellationToken cancellationToken);
}

public class AgentDividendDistributionTaskQueue : IAgentDividendDistributionTaskQueue
{
    private readonly Channel<AgentDividendDistributionQueueModel> _queue;

    public AgentDividendDistributionTaskQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true
        };
        _queue = Channel
            .CreateUnbounded<AgentDividendDistributionQueueModel>(options);
    }

    public async ValueTask QueueAgentDividendDistributionAsync(
        AgentDividendDistributionQueueModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await _queue.Writer.WriteAsync(model);
    }

    public async ValueTask<AgentDividendDistributionQueueModel>
        DequeueAsync(
            CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}