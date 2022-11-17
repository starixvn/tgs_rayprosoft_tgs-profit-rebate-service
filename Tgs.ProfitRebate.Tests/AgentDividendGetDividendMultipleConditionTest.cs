using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class AgentDividendGetDividendMultipleConditionTest
{
    [Test]
    public async Task Can_Not_Get_Agent_Dividends_With_No_Condition()
    {
        var fakeAgentDividendService = Substitute.For<ITgsService<AgentDividend>>();
        var fakeAgentService = Substitute.For<ITgsService<Agent>>();
        var fakeAgentWalletRepository = Substitute.For<IAgentWalletRepository>();
        var fakeBrandService = Substitute.For<ITgsService<Brand>>();
        var fakePlayerAggregateService = Substitute.For<ITgsService<PlayerAggregate>>();
        var fakePlayerBonusAggregateService = Substitute.For<ITgsService<PlayerBonusAggregate>>();
        var fakePlayerGameRebateAggregateService = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();

        var sut = new AgentDividendFactory(fakeAgentService, fakeBrandService, fakeAgentDividendService,
            fakePlayerAggregateService, fakePlayerGameRebateAggregateService, fakePlayerBonusAggregateService,
            fakeAgentWalletRepository);

        var from = DateTimeOffset.Parse("2022-10-01 00:00:00+8");
        var to = DateTimeOffset.Parse("2022-10-31 00:00:00+8");

        var brandData = CreateBrandFixture(from, to)
            .CreateMany<Brand>(1)
            .AsQueryable()
            .BuildMock();

        var brand = brandData.First();

        var agentData = CreateAgentFixture(brand, null)
            .CreateMany<Agent>(1)
            .AsQueryable()
            .BuildMock();

        var agent = agentData.First();

        var playerData = CreatePlayerFixture(brand, agent)
            .CreateMany<Player>(1)
            .AsQueryable()
            .BuildMock();

        var player = playerData.First();

        var playerAggregateData = CreatePlayerAggregateFixture(brand, agent, player, from, 100)
            .CreateMany<PlayerAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerBonusAggregateData = CreatePlayerBonusAggregateFixture(brand, agent, player, from)
            .CreateMany<PlayerBonusAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerGameAggregateData = CreatePlayerGameAggregateFixture(brand, agent, player, from)
            .CreateMany<PlayerGameRebateAggregate>(1)
            .AsQueryable()
            .BuildMock();

        fakeBrandService.GetQueries().Returns(brandData);
        fakeAgentService.GetQueries().Returns(agentData);
        fakePlayerAggregateService.GetQueries().Returns(playerAggregateData);
        fakePlayerBonusAggregateService.GetQueries().Returns(playerBonusAggregateData);
        fakePlayerGameRebateAggregateService.GetQueries().Returns(playerGameAggregateData);
        fakeAgentDividendService.GetQueries().Returns(Array.Empty<AgentDividend>().AsQueryable().BuildMock());

        var (dividends, manualDividends, _, _) = await sut.GetAgentDividends(brandData.First(), 0, default);

        Assert.IsTrue(dividends.Count + manualDividends.Count == 1);

        foreach (var dividend in dividends.Concat(manualDividends))
        {
            var agentAdjustAmount = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.AgentAdjustAmount - x.NoIncludeNetProfitAgentAdjustAmount);

            Assert.IsTrue(dividend.AgentDividendAmount == -agentAdjustAmount);
        }
    }

    [Test]
    public async Task Can_Get_Agent_Dividends_With_Rank_1_Condition()
    {
        var fakeAgentDividendService = Substitute.For<ITgsService<AgentDividend>>();
        var fakeAgentService = Substitute.For<ITgsService<Agent>>();
        var fakeAgentWalletRepository = Substitute.For<IAgentWalletRepository>();
        var fakeBrandService = Substitute.For<ITgsService<Brand>>();
        var fakePlayerAggregateService = Substitute.For<ITgsService<PlayerAggregate>>();
        var fakePlayerBonusAggregateService = Substitute.For<ITgsService<PlayerBonusAggregate>>();
        var fakePlayerGameRebateAggregateService = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();

        var sut = new AgentDividendFactory(fakeAgentService, fakeBrandService, fakeAgentDividendService,
            fakePlayerAggregateService, fakePlayerGameRebateAggregateService, fakePlayerBonusAggregateService,
            fakeAgentWalletRepository);

        var from = DateTimeOffset.Parse("2022-10-01 00:00:00+8");
        var to = DateTimeOffset.Parse("2022-10-31 00:00:00+8");

        var brandData = CreateBrandFixture(from, to)
            .CreateMany<Brand>(1)
            .AsQueryable()
            .BuildMock();

        var brand = brandData.First();

        var agentData = CreateAgentFixture(brand, null)
            .CreateMany<Agent>(1)
            .AsQueryable()
            .BuildMock();

        var agent = agentData.First();

        var playerData = CreatePlayerFixture(brand, agent)
            .CreateMany<Player>(1)
            .AsQueryable()
            .BuildMock();

        var player = playerData.First();

        var playerAggregateData = CreatePlayerAggregateFixture(brand, agent, player, from, 200)
            .CreateMany<PlayerAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerBonusAggregateData = CreatePlayerBonusAggregateFixture(brand, agent, player, from)
            .CreateMany<PlayerBonusAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerGameAggregateData = CreatePlayerGameAggregateFixture(brand, agent, player, from)
            .CreateMany<PlayerGameRebateAggregate>(1)
            .AsQueryable()
            .BuildMock();

        fakeBrandService.GetQueries().Returns(brandData);
        fakeAgentService.GetQueries().Returns(agentData);
        fakePlayerAggregateService.GetQueries().Returns(playerAggregateData);
        fakePlayerBonusAggregateService.GetQueries().Returns(playerBonusAggregateData);
        fakePlayerGameRebateAggregateService.GetQueries().Returns(playerGameAggregateData);
        fakeAgentDividendService.GetQueries().Returns(Array.Empty<AgentDividend>().AsQueryable().BuildMock());

        var (dividends, manualDividends, _, _) = await sut.GetAgentDividends(brandData.First(), 0, default);

        Assert.IsTrue(dividends.Count + manualDividends.Count == 1);

        foreach (var dividend in dividends.Concat(manualDividends))
        {
            var winLoss = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.WinLossAmount);

            var aggBonus = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.RebateAmount + x.PromotionAmount);

            var bonusAdjust = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.BonusAdjustAmount - x.NoIncludeNetProfitBonusAdjustAmount);

            var bonus = playerBonusAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.BirthdayAmount + x.UpgradeAmount + x.SeasonAmount);

            var agentAdjustAmount = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.AgentAdjustAmount - x.NoIncludeNetProfitAgentAdjustAmount);

            var percentage = agentData.First(x => x.Id == dividend.AgentId).DividendPercentage;

            Assert.IsTrue(dividend.AgentDividendAmount == (winLoss - aggBonus - bonus - bonusAdjust - winLoss * brand.PlatformCharge / 100) * 10 / 100 - agentAdjustAmount);
        }
    }

    [Test]
    public async Task Can_Get_Agent_Dividends_With_Rank_2_Condition()
    {
        var fakeAgentDividendService = Substitute.For<ITgsService<AgentDividend>>();
        var fakeAgentService = Substitute.For<ITgsService<Agent>>();
        var fakeAgentWalletRepository = Substitute.For<IAgentWalletRepository>();
        var fakeBrandService = Substitute.For<ITgsService<Brand>>();
        var fakePlayerAggregateService = Substitute.For<ITgsService<PlayerAggregate>>();
        var fakePlayerBonusAggregateService = Substitute.For<ITgsService<PlayerBonusAggregate>>();
        var fakePlayerGameRebateAggregateService = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();

        var sut = new AgentDividendFactory(fakeAgentService, fakeBrandService, fakeAgentDividendService,
            fakePlayerAggregateService, fakePlayerGameRebateAggregateService, fakePlayerBonusAggregateService,
            fakeAgentWalletRepository);

        var from = DateTimeOffset.Parse("2022-10-01 00:00:00+8");
        var to = DateTimeOffset.Parse("2022-10-31 00:00:00+8");

        var brandData = CreateBrandFixture(from, to)
            .CreateMany<Brand>(1)
            .AsQueryable()
            .BuildMock();

        var brand = brandData.First();

        var agentData = CreateAgentFixture(brand, null)
            .CreateMany<Agent>(1)
            .AsQueryable()
            .BuildMock();

        var agent = agentData.First();

        var playerData = CreatePlayerFixture(brand, agent)
            .CreateMany<Player>(1)
            .AsQueryable()
            .BuildMock();

        var player = playerData.First();

        var playerAggregateData = CreatePlayerAggregateFixture(brand, agent, player, from, 300)
            .CreateMany<PlayerAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerBonusAggregateData = CreatePlayerBonusAggregateFixture(brand, agent, player, from)
            .CreateMany<PlayerBonusAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerGameAggregateData = CreatePlayerGameAggregateFixture(brand, agent, player, from)
            .CreateMany<PlayerGameRebateAggregate>(1)
            .AsQueryable()
            .BuildMock();

        fakeBrandService.GetQueries().Returns(brandData);
        fakeAgentService.GetQueries().Returns(agentData);
        fakePlayerAggregateService.GetQueries().Returns(playerAggregateData);
        fakePlayerBonusAggregateService.GetQueries().Returns(playerBonusAggregateData);
        fakePlayerGameRebateAggregateService.GetQueries().Returns(playerGameAggregateData);
        fakeAgentDividendService.GetQueries().Returns(Array.Empty<AgentDividend>().AsQueryable().BuildMock());

        var (dividends, manualDividends, _, _) = await sut.GetAgentDividends(brandData.First(), 0, default);

        Assert.IsTrue(dividends.Count + manualDividends.Count == 1);

        foreach (var dividend in dividends.Concat(manualDividends))
        {
            var winLoss = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.WinLossAmount);

            var aggBonus = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.RebateAmount + x.PromotionAmount);

            var bonusAdjust = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.BonusAdjustAmount - x.NoIncludeNetProfitBonusAdjustAmount);

            var bonus = playerBonusAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.BirthdayAmount + x.UpgradeAmount + x.SeasonAmount);

            var agentAdjustAmount = playerAggregateData.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.AgentAdjustAmount - x.NoIncludeNetProfitAgentAdjustAmount);

            var percentage = agentData.First(x => x.Id == dividend.AgentId).DividendPercentage;

            Assert.IsTrue(dividend.AgentDividendAmount == (winLoss - aggBonus - bonus - bonusAdjust - winLoss * brand.PlatformCharge / 100) * 20 / 100 - agentAdjustAmount);
        }
    }


    private Fixture CreateBrandFixture(DateTimeOffset from, DateTimeOffset to)
    {
        var fixture = new Fixture();
        fixture.Customize<Brand>(u => u
            .With(s => s.AutoSentAgentBonus, true)
            .With(s => s.AgentDividendType, (short)AgentDividendType.MultipleLayer)
            .With(s => s.CheckDividendActiveCount, true)
            .With(s => s.CurrentSettlementFrom, from.UtcDateTime)
            .With(s => s.CurrentSettlementTo, to.UtcDateTime)
            .With(s => s.PlatformCharge, 0)
            .With(s => s.Agents, new Agent[] { })
            .With(s => s.Players, new Player[] { })
            .With(s => s.Promotions, new Promotion[] { })
            .With(s => s.Transactions, new Transaction[] { })
            .With(s => s.VipLevels, new VipLevel[] { })
            .With(s => s.Licensee, new Licensee())
            .With(s => s.AgentTransactions, new AgentTransaction[] { })
            .With(s => s.CurrentSettlementTo, new DateTimeOffset(2022, 10, 1, 0, 0, 0, new(8, 0, 0)).UtcDateTime));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateAgentFixture(Brand brand, Agent? parentAgent)
    {
        var agentId = Guid.NewGuid();
        var condition = new AgentDividendCondition[]
        {
            new AgentDividendCondition
            {
                Rank = 1,
                AgentId = agentId,
                NetProfit = 100,
                ActiveNumber = 1,
                DividendPercentage = 10,
            },
            new AgentDividendCondition
            {
                Rank = 2,
                AgentId = agentId,
                NetProfit = 200,
                ActiveNumber = 1,
                DividendPercentage = 20,
            }
        };

        var fixture = new Fixture();

        fixture.Customize<Agent>(u => u
            .With(s => s.Id, agentId)
            .With(s => s.Brand, brand)
            .With(s => s.ParentAgentId, parentAgent?.Id)
            .With(s => s.DividendPercentage, 10)
            .With(s => s.AgentWallets, new AgentWallet[] { })
            .With(s => s.Players, new Player[] { })
            .With(s => s.AgentDividendConditions, new AgentDividendCondition[] { })
            .With(s => s.InverseParentAgent, new Agent[] { })
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentDividendConditions, condition));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerFixture(Brand brand, Agent? agent)
    {
        var fixture = new Fixture();
        fixture.Customize<Player>(u => u
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentId, agent?.Id)
            .With(s => s.Brand, brand)
            .With(s => s.Agent, agent)
            .With(s => s.Birthdate, DateOnly.FromDateTime(DateTime.Now))
            .With(s => s.Wallets, new Wallet[] { })
        );
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerAggregateFixture(Brand brand, Agent? agent, Player player, DateTimeOffset reportDate, decimal winLoss)
    {
        var fixture = new Fixture();
        var percentageJson =
            "[{\"AgentId\":\""+ agent!.Id + "\",\"ParentAgentId\":null,\"DividendPercentage\":"+ agent.DividendPercentage +"}]";
        fixture.Customize<PlayerAggregate>(u => u
            .With(s => s.ReportDate, reportDate.UtcDateTime)
            .With(s => s.Hour, 0)
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentId, agent?.Id)
            .With(s => s.PlayerCode, player.PlayerCode)
            .With(s => s.WinLossAmount, winLoss)
            .With(s => s.RebateAmount, 10)
            .With(s => s.PromotionAmount, 10)
            .With(s => s.AgentAdjustAmount, 5)
            .With(s => s.NoIncludeNetProfitAgentAdjustAmount, 0)
            .With(s => s.BonusAdjustAmount, 10)
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0)
            .With(s => s.PromotionAmount, 10)
            .With(s => s.AgentDividendPercentageTreeData, percentageJson)
            .With(s => s.AgentDividendPercentage, agent?.DividendPercentage ?? 0));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerBonusAggregateFixture(Brand brand, Agent? agent, Player player, DateTimeOffset reportDate)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerBonusAggregate>(u => u
            .With(s => s.ReportDate, reportDate.UtcDateTime)
            .With(s => s.Hour, 0)
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentId, agent?.Id)
            .With(s => s.RebateAmount, 10)
            .With(s => s.BirthdayAmount, 10)
            .With(s => s.UpgradeAmount, 10)
            .With(s => s.SeasonAmount, 10)
            .With(s => s.PlayerCode, player.PlayerCode));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerGameAggregateFixture(Brand brand, Agent? agent, Player player, DateTimeOffset reportDate)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerGameRebateAggregate>(u => u
            .With(s => s.ReportDate, reportDate.UtcDateTime)
            .With(s => s.Hour, 0)
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentId, agent?.Id)
            .With(s => s.PlayerCode, player.PlayerCode));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}