using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.ViewModel;

public record CreatePlayerBucketRequest(
	string PlayerCode,
	BucketType BucketType,
	decimal BucketAmount,
	string? ExternalId);
