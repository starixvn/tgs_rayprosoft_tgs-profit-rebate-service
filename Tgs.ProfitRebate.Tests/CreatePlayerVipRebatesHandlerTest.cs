using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class CreatePlayerVipRebatesHandlerTest
{
    [Test]
    public async Task Create_Vip_Player_Rebates()
    {
        var fakeBrandService = Substitute.For<ITgsService<Brand>>();
        var fakePlayerGameAggregateService = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
        var fakePlayerService = Substitute.For<ITgsService<Player>>();
        var fakeBrandGameService = Substitute.For<ITgsService<BrandGame>>();
        var fakeTaskQueue = Substitute.For<IPlayerRebateTaskQueue>();

        var sut = new CreatePlayerVipRebatesHandler(fakeBrandService, fakePlayerGameAggregateService, fakePlayerService,
            fakeBrandGameService, fakeTaskQueue);

        var request = new CreateVipPlayerRebatesRequest(null);

        var playerGameRebateAggregates = CreatePlayerGameAggregateFixture(DateTime.UtcNow)
            .CreateMany<PlayerGameRebateAggregate>(3).AsQueryable().BuildMock();

        var brands = playerGameRebateAggregates.Select(x => x.BrandId).Distinct()
            .Select(x => CreateBrandFixture(x).Create<Brand>()).AsQueryable().BuildMock();

        var players = playerGameRebateAggregates.Select(x => new { x.BrandId, x.PlayerCode }).Distinct()
            .Select(x => CreatePlayerFixture(x.BrandId, x.PlayerCode).Create<Player>()).AsQueryable().BuildMock();

        fakePlayerGameAggregateService
            .GetQueries(Arg.Any<Func<IQueryable<PlayerGameRebateAggregate>, IQueryable<PlayerGameRebateAggregate>>>(),
                Arg.Any<bool>()).Returns(playerGameRebateAggregates);
        fakeBrandService.GetQueries().Returns(brands);
        fakePlayerService.GetQueries().Returns(players);

        _ = await sut.Handle(request, default);

        _ = fakeTaskQueue.Received(3)
            .QueuePlayerRebateAsync(Arg.Any<PlayerRebateQueueModel>());
    }

    private static Fixture CreateBrandFixture(
        Guid brandId)
    {
        var fixture = new Fixture();
        fixture.Customize<Brand>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.AgentDividendType, (short)AgentDividendType.MultipleLayer)
            .With(s => s.CheckDividendActiveCount, true)
            .With(s => s.CurrentSettlementFrom, DateTime.UtcNow)
            .With(s => s.CurrentSettlementTo, DateTime.UtcNow)
            .With(s => s.PlatformCharge, 0)
            .With(s => s.Agents, Array.Empty<Agent>())
            .With(s => s.Players, Array.Empty<Player>())
            .With(s => s.Promotions, Array.Empty<Promotion>())
            .With(s => s.Transactions, Array.Empty<Transaction>())
            .With(s => s.VipLevels, Array.Empty<VipLevel>())
            .With(s => s.Licensee, new Licensee())
            .With(s => s.AgentTransactions, Array.Empty<AgentTransaction>()));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreatePlayerFixture(
        Guid brandId,
        string playerCode)
    {
        var fixture = new Fixture();
        fixture.Customize<Player>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.PlayerCode, playerCode)
            .With(s => s.Birthdate, DateOnly.FromDateTime(DateTime.Now))
        );

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerGameAggregateFixture(DateTimeOffset reportDate)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerGameRebateAggregate>(u => u
            .With(s => s.ReportDate, reportDate.UtcDateTime)
            .With(s => s.Hour, 0));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}