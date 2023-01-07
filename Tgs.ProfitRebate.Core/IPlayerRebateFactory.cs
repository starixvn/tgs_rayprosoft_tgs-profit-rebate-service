using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Core;

public interface IPlayerRebateFactory
{
    ValueTask CreatePlayerBucketsAsync(params CreatePlayerBucketRequest[] requests);

	ValueTask<PlayerRebate> CreateVipPlayerRebateAsync(VipPlayerRebateModel model, DateTimeOffset? sentOn);
}
