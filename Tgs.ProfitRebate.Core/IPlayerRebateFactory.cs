using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core
{
    public interface IPlayerRebateFactory
    {
        ValueTask<PlayerRebate[]> CreateVipPlayerRebatesAsync(VipPlayerRebateModel[] models, DateTimeOffset? sentOn);
        ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models, DateTimeOffset? sentOn);
        ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests);
        ValueTask<PlayerAllAgentTurnoverWinLossModel[]> GetPlayerTurnoverWinLossAsync(DateTimeOffset reportDate);
        ValueTask<BrandAllAgentGameTypeConditionModel[]> GetBrandAllAgentGameTypeConditionsAsync(Guid[] brandIds);
    }
}