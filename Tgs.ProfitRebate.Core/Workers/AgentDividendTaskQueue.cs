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

	public AgentDividendTaskQueue(int capacity)
	{
		var options = new BoundedChannelOptions(capacity)
		{
			FullMode = BoundedChannelFullMode.Wait
		};
		_queue = Channel
			.CreateBounded<AgentDividendQueueModel>(options);
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
