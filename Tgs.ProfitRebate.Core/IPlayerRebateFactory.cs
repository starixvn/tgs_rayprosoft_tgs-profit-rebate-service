using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core
{
    public interface IPlayerRebateFactory
    {
        ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models, DateTimeOffset? sentOn);
        ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests);
        ValueTask<PlayerAllAgentTurnoverWinLossModel[]> GetPlayerTurnoverWinLossAsync(DateTimeOffset reportDate);
        ValueTask<BrandAllAgentGameTypeConditionModel[]> GetBrandAllAgentGameTypeConditionsAsync(Guid[] brandIds);
        ValueTask<PlayerRebate> CreateVipPlayerRebateAsync(VipPlayerRebateModel model, DateTimeOffset? sentOn);
        ValueTask CreatePlayerBucketAsync(CreatePlayerBucketRequest requests);
    }
}