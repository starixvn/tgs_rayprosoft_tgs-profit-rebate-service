using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Core;

public interface IPlayerRebateFactory
{
    ValueTask CreateAllAgentPlayerRebateAsync(PlayerAllAgentRebateModel model);

    ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models);

    ValueTask CreatePlayerBucketsAsync(params CreatePlayerBucketRequest[] requests);

	ValueTask<PlayerAllAgentTurnoverWinLossModel[]> GetPlayerTurnoverWinLossAsync(DateTimeOffset reportDate);

	ValueTask<BrandAllAgentGameTypeConditionModel[]> GetBrandAllAgentGameTypeConditionsAsync(Guid[] brandIds);

	ValueTask<PlayerRebate> CreateVipPlayerRebateAsync(VipPlayerRebateModel model, DateTimeOffset? sentOn);

    ValueTask<ValueTuple<Guid, string>[]> GetValidAllAgentPlayerCodesAsync(DateTimeOffset reportDate);

    ValueTask<PlayerAllAgentTurnoverWinLossModel[]> GetPlayerAllAgentTurnoverWinLossAsync(DateTimeOffset reportDate, string parentPlayerCode);

}
