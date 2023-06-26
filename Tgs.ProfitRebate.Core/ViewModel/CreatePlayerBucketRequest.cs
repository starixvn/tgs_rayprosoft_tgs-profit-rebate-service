using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core.ViewModel;

public record CreatePlayerBucketRequest(
    string PlayerCode,
    BucketType BucketType,
    decimal BucketAmount,
    string? ExternalId);