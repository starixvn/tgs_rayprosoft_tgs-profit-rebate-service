using System.Threading.Channels;

namespace Tgs.ProfitRebate.Core.Workers;

public interface IPlayerRebateTaskQueue
{
	ValueTask QueuePlayerRebateAsync(
		PlayerRebateQueueModel model);

	ValueTask<PlayerRebateQueueModel> DequeueAsync(
		CancellationToken cancellationToken);
}

public class PlayerRebateTaskQueue : IPlayerRebateTaskQueue
{
	private readonly Channel<PlayerRebateQueueModel> _queue;

	public PlayerRebateTaskQueue(int capacity)
	{
		var options = new BoundedChannelOptions(capacity)
		{
			FullMode = BoundedChannelFullMode.Wait
		};
		_queue = Channel
			.CreateBounded<PlayerRebateQueueModel>(options);
	}

	public async ValueTask QueuePlayerRebateAsync(
		PlayerRebateQueueModel model)
	{
		if (model == null)
		{
			throw new ArgumentNullException(nameof(model));
		}

		await _queue.Writer.WriteAsync(model);
	}

	public async ValueTask<PlayerRebateQueueModel>
		DequeueAsync(
			CancellationToken cancellationToken)
	{
		var workItem = await _queue.Reader.ReadAsync(cancellationToken);

		return workItem;
	}
}
