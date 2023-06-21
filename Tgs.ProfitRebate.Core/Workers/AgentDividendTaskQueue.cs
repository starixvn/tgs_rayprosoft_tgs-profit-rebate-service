using System.Threading.Channels;

namespace Tgs.ProfitRebate.Core.Workers;

public interface IAgentDividendTaskQueue
{
    ValueTask QueueAgentDividendAsync(
        AgentDividendQueueModel model);

    ValueTask<AgentDividendQueueModel> DequeueAsync(
        CancellationToken cancellationToken);
}

public class AgentDividendTaskQueue : IAgentDividendTaskQueue
{
    private readonly Channel<AgentDividendQueueModel> _queue;

    public AgentDividendTaskQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true
        };
        _queue = Channel
            .CreateUnbounded<AgentDividendQueueModel>(options);
    }

    public async ValueTask QueueAgentDividendAsync(
        AgentDividendQueueModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await _queue.Writer.WriteAsync(model);
    }

    public async ValueTask<AgentDividendQueueModel>
        DequeueAsync(
            CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}