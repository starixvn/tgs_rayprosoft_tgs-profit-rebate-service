using System.Threading.Channels;

namespace Tgs.ProfitRebate.Core.Workers
{
    public interface IAgentDividendTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(
            (Func<AgentDividendQueueModel, ValueTask> func, AgentDividendQueueModel model) workItem);

        ValueTask<(Func<AgentDividendQueueModel, ValueTask> func, AgentDividendQueueModel model)> DequeueAsync(
            CancellationToken cancellationToken);
    }

    public class AgentDividendTaskQueue : IAgentDividendTaskQueue
    {
        private readonly Channel<(Func<AgentDividendQueueModel, ValueTask>, AgentDividendQueueModel)> _queue;

        public AgentDividendTaskQueue(int capacity)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel
                .CreateBounded<(Func<AgentDividendQueueModel, ValueTask>, AgentDividendQueueModel)>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(
            (Func<AgentDividendQueueModel, ValueTask> func, AgentDividendQueueModel model) workItem)
        {
            if (workItem.func == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<(Func<AgentDividendQueueModel, ValueTask> func, AgentDividendQueueModel model)>
            DequeueAsync(
                CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);

            return (workItem.Item1, workItem.Item2);
        }
    }
}