using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.ProfitRebate.Core;

public class PlayerRebateFactory : IPlayerRebateFactory
{
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly ITgsService<PlayerRebate> _playerRebateService;

	public PlayerRebateFactory(
		ITgsService<PlayerRebate> playerRebateService,
        IPlayerBucketClient playerBucketClient)
	{
		_playerRebateService = playerRebateService ?? throw new ArgumentNullException(nameof(playerRebateService));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

    public async ValueTask CreatePlayerBucketsAsync(CreatePlayerBucketRequest[] requests)
	{
		foreach (var request in requests)
			_ = await _playerBucketClient.CreateBucketAsync(
				request.PlayerCode,
				request.BucketAmount,
				request.BucketType,
				request.ExternalId,
                "").ConfigureAwait(false);
	}

	public async ValueTask<PlayerRebate> CreateVipPlayerRebateAsync(VipPlayerRebateModel model, DateTimeOffset? sentOn)
	{
		var rebate = new PlayerRebate
		{
			BrandId = model.BrandId,
			AgentId = model.AgentId,
			PlayerCode = model.PlayerCode,
			ReportDate = model.ReportDate.UtcDateTime,
			CreatedOn = DateTime.UtcNow,
			SentOn = sentOn?.UtcDateTime,
			VipCardGameRebatePercentage = model.VipCardGameRebatePercentage,
			VipLiveDealerRebatePercentage = model.VipLiveDealerRebatePercentage,
			VipLotteryRebatePercentage = model.VipLotteryRebatePercentage,
			VipSlotRebatePercentage = model.VipSlotRebatePercentage,
			VipSportsRebatePercentage = model.VipSportsRebatePercentage,
			VipVirtualSportsRebatePercentage = model.VipVirtualSportsRebatePercentage,
			SportsTurnover = model.SportsTurnover,
			SportsRebateAmount = model.SportsRebateAmount,
			VirtualSportsTurnover = model.VirtualSportsTurnover,
			VirtualSportsRebateAmount = model.VirtualSportsRebateAmount,
			LiveDealerTurnover = model.LiveDealerTurnover,
			LiveDealerRebateAmount = model.LiveDealerRebateAmount,
			CardGameTurnover = model.CardGameTurnover,
			CardGameRebateAmount = model.CardGameRebateAmount,
			SlotTurnover = model.SlotTurnover,
			SlotRebateAmount = model.SlotRebateAmount,
			LotteryTurnover = model.LotteryTurnover,
			LotteryRebateAmount = model.LotteryRebateAmount,
			RebateAmount = model.RebateAmount
		};

		await _playerRebateService.CreateEntityAsync(rebate);

		return rebate;
	}
}
