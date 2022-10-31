using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.ViewModel
{
	public record CreatePlayerBucketRequest(
		string PlayerCode,
		BucketType BucketType,
		decimal BucketAmount);
}
