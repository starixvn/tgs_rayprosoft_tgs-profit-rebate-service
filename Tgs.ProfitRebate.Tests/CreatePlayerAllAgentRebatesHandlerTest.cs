using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class CreatePlayerAllAgentRebatesHandlerTest
{
    [Test]
    public async Task Can_Create_All_Agent_Rebates_EnQueue()
    {
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();

        var sut = new CreatePlayerAllAgentRebatesHandler(fakePlayerRebateFactory, fakePlayerAllAgentRebateTaskQueue);

        var request = new CreatePlayerAllAgentRebatesRequest(null);

        var fixture = new Fixture();

        fixture.Customize<PlayerAllAgentTurnoverWinLossModel>(u
            => u.With(x => x.GameType, GameType.Sportsbook));

        var playerAllAgentTurnoverWinLoss = CreatePlayerAllAgentTurnoverWinLossModelFixture(GameType.Sportsbook)
            .CreateMany<PlayerAllAgentTurnoverWinLossModel>(1);

        var firstPlayerAllAgentTurnoverWinLoss = playerAllAgentTurnoverWinLoss.First();

        var brandAllAgentGameTypeConditions =
            CreateBrandAllAgentGameTypeConditionModelFixture(firstPlayerAllAgentTurnoverWinLoss)
                .CreateMany<BrandAllAgentGameTypeConditionModel>(1);

        var validPlayerCodes = new[] { (Guid.NewGuid(), "playerCode") };

        _ = fakePlayerRebateFactory.GetValidAllAgentPlayerCodesAsync(Arg.Any<DateTimeOffset>())
            .Returns(validPlayerCodes);

        _ = fakePlayerRebateFactory.GetBrandAllAgentGameTypeConditionsAsync(Arg.Any<Guid[]>())
            .Returns(brandAllAgentGameTypeConditions.ToArray());

        _ = await sut.Handle(request, default);

        await fakePlayerAllAgentRebateTaskQueue.Received(1)
            .QueuePlayerRebateAsync(Arg.Any<PlayerAllAgentRebateQueueModel>());
    }

    private static Fixture CreatePlayerAllAgentTurnoverWinLossModelFixture(GameType type)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerAllAgentTurnoverWinLossModel>(u
            => u.With(x => x.PlayerCode, "playerCode")
                .With(x => x.GameType, type));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateBrandAllAgentGameTypeConditionModelFixture(
        PlayerAllAgentTurnoverWinLossModel model)
    {
        var fixture = new Fixture();
        fixture.Customize<BrandAllAgentGameTypeConditionModel>(u
            => u.With(x => x.BrandId, model.BrandId)
                .With(x => x.AllAgentGameType, AllAgentGameType.Sportsbook)
                .With(x => x.NetProfit, model.Turnover)
                .With(x => x.HasValidBet, true)
                .With(x => x.HasWinLoss, true));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}