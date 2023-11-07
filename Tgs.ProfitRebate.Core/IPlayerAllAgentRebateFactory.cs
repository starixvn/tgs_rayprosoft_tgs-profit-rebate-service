using Tgs.ProfitRebate.Core.ViewModel;

namespace Tgs.ProfitRebate.Core;

public interface IPlayerAllAgentRebateFactory
{
    ValueTask CreateAllAgentPlayerRebatesAsync(PlayerAllAgentRebateModel[] models);

    ValueTask CreatePlayerBucketsAsync(params CreatePlayerBucketRequest[] requests);

    ValueTask<BrandAllAgentGameTypeConditionModel[]> GetBrandAllAgentGameTypeConditionsAsync(Guid[] brandIds);

    ValueTask<ValueTuple<Guid, string>[]> GetValidAllAgentPlayerCodesAsync(DateTimeOffset reportDate);

    ValueTask<PlayerAllAgentTurnoverWinLossModel[]> GetPlayerAllAgentTurnoverWinLossAsync(DateTimeOffset reportDate,
        string parentPlayerCode);

    ValueTask<Guid> CreateAllAgentPlayerRebateContributorAsync(PlayerAllAgentRebateContributorModel model);
}