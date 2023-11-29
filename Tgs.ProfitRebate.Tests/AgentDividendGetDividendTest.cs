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
public class AgentDividendGetDividendTest
{
    [Test]
    public async Task Can_Get_Agent_Dividends()
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

        var agentData = CreateAgentFixture(brand, 90, null)
            .CreateMany<Agent>(1)
            .AsQueryable()
            .BuildMock();

        var agent = agentData.First();

        var playerData = CreatePlayerFixture(brand, agent)
            .CreateMany<Player>(1)
            .AsQueryable()
            .BuildMock();

        var player = playerData.First();

        var playerAggregateData = CreatePlayerAggregateFixture(brand, agent, 90, player, from, null)
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

        var (dividends, _) = await sut.GetAgentDividendsAsync(brandData.First(), 0, default);

        var (sendDividends, _) = await sut.GetNoSendDividendsAsync(dividends, default);

        Assert.IsTrue(sendDividends.Count == 1);

        foreach (var dividend in dividends.Concat(sendDividends))
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

            Assert.IsTrue(dividend.AgentDividendAmount ==
                (winLoss - bonus - aggBonus - bonusAdjust - winLoss * brand.PlatformCharge / 100) * percentage / 100 -
                agentAdjustAmount);
        }
    }

    [Test]
    public async Task Can_Get_Agent_Dividends_When_Changed_Up_Layer_Agent()
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

        var chiefAgent1 = CreateAgentFixture(brand, 80, null).Create<Agent>();
        var chiefAgent2 = CreateAgentFixture(brand, 90, null).Create<Agent>();

        var childAgent1 = CreateAgentFixture(brand, 20, chiefAgent1).Create<Agent>();
        var childAgent2 = CreateAgentFixture(brand, 50, chiefAgent2).Create<Agent>();

        var agentData = new Agent[]
            {
                chiefAgent1,
                chiefAgent2,
                childAgent1,
                childAgent2
            }
            .AsQueryable()
            .BuildMock();

        var childAgent2PlayerData = CreatePlayerFixture(brand, childAgent2)
            .CreateMany<Player>(1)
            .AsQueryable()
            .BuildMock();

        var player = childAgent2PlayerData.First();

        var playerAggregateData = new PlayerAggregate[]
            {
                CreatePlayerAggregateFixture(brand, childAgent2, 30, player, from, $$"""
                                                                                 [
                                                                                 {"AgentId":"{{childAgent2.Id}}","ParentAgentId":"{{chiefAgent1.Id}}","DividendPercentage":30},
                                                                                 {"AgentId":"{{chiefAgent1.Id}}","ParentAgentId":null,"DividendPercentage":80}]
                                                                                 """).Create<PlayerAggregate>(),
                CreatePlayerAggregateFixture(brand, childAgent2, 50, player, from, $$"""
                                                                                 [
                                                                                 {"AgentId":"{{childAgent2.Id}}","ParentAgentId":"{{chiefAgent2.Id}}","DividendPercentage":50},
                                                                                 {"AgentId":"{{chiefAgent2.Id}}","ParentAgentId":null,"DividendPercentage":90}]
                                                                                 """).Create<PlayerAggregate>()
            }.AsQueryable()
            .BuildMock();

        var playerBonusAggregateData = CreatePlayerBonusAggregateFixture(brand, childAgent2, player, from)
            .CreateMany<PlayerBonusAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerGameAggregateData = CreatePlayerGameAggregateFixture(brand, childAgent2, player, from)
            .CreateMany<PlayerGameRebateAggregate>(1)
            .AsQueryable()
            .BuildMock();

        fakeBrandService.GetQueries().Returns(brandData);
        fakeAgentService.GetQueries().Returns(agentData);
        fakePlayerAggregateService.GetQueries().Returns(playerAggregateData);
        fakePlayerBonusAggregateService.GetQueries().Returns(playerBonusAggregateData);
        fakePlayerGameRebateAggregateService.GetQueries().Returns(playerGameAggregateData);
        fakeAgentDividendService.GetQueries().Returns(Array.Empty<AgentDividend>().AsQueryable().BuildMock());

        var (dividends, _) = await sut.GetAgentDividendsAsync(brandData.First(), 0, default);

        var (sendDividends, _) = await sut.GetNoSendDividendsAsync(dividends, default);

        Assert.IsTrue(sendDividends.Count == 5);

        foreach (var dividend in sendDividends)
        {
            var agentTotalDividendAmount = sendDividends.SelectMany(x => x.AgentDividendMultipleLayerModels)
                .Where(x => x.AgentId == dividend.AgentId && x.AgentDividendPercentage == dividend.AgentDividendPercentage)
                .Sum(x => x.Dividend);

            Assert.IsTrue(dividend.AgentDividendAmount == agentTotalDividendAmount - (dividend.AgentAdjustAmount - dividend.NoIncludeNetProfitAgentAdjustAmount));
        }
    }

    [Test]
    public async Task Can_Deduct_Negative_Dividends()
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

        var chiefAgent1 = CreateAgentFixture(brand, 80, null).Create<Agent>();
        var chiefAgent2 = CreateAgentFixture(brand, 90, null).Create<Agent>();

        var childAgent1 = CreateAgentFixture(brand, 20, chiefAgent1).Create<Agent>();
        var childAgent2 = CreateAgentFixture(brand, 50, chiefAgent2).Create<Agent>();

        var agentData = new Agent[]
            {
                chiefAgent1,
                chiefAgent2,
                childAgent1,
                childAgent2
            }
            .AsQueryable()
            .BuildMock();

        var childAgent2PlayerData = CreatePlayerFixture(brand, childAgent2)
            .CreateMany<Player>(1)
            .AsQueryable()
            .BuildMock();

        var player = childAgent2PlayerData.First();

        var playerAggregateData = new PlayerAggregate[]
            {
                CreatePlayerAggregateFixture(brand, childAgent2, 30, player, from, $$"""
                                                                                 [
                                                                                 {"AgentId":"{{childAgent2.Id}}","ParentAgentId":"{{chiefAgent1.Id}}","DividendPercentage":30},
                                                                                 {"AgentId":"{{chiefAgent1.Id}}","ParentAgentId":null,"DividendPercentage":80}]
                                                                                 """).Create<PlayerAggregate>(),
                CreatePlayerAggregateFixture(brand, childAgent2, 50, player, from, $$"""
                                                                                 [
                                                                                 {"AgentId":"{{childAgent2.Id}}","ParentAgentId":"{{chiefAgent2.Id}}","DividendPercentage":50},
                                                                                 {"AgentId":"{{chiefAgent2.Id}}","ParentAgentId":null,"DividendPercentage":90}]
                                                                                 """).Create<PlayerAggregate>()
            }.AsQueryable()
            .BuildMock();

        var negativeDividends = new AgentDividend[]
        {
            new AgentDividend
            {
                AgentId = childAgent2.Id,
                AgentDividendAmount = -10,
                SentOn = null
            },
            new AgentDividend
            {
                AgentId = childAgent2.Id,
                AgentDividendAmount = -20,
                SentOn = null
            },
            new AgentDividend
            {
                AgentId = childAgent2.Id,
                AgentDividendAmount = -30,
                SentOn = null
            }
        };

        var playerBonusAggregateData = CreatePlayerBonusAggregateFixture(brand, childAgent2, player, from)
            .CreateMany<PlayerBonusAggregate>(1)
            .AsQueryable()
            .BuildMock();

        var playerGameAggregateData = CreatePlayerGameAggregateFixture(brand, childAgent2, player, from)
            .CreateMany<PlayerGameRebateAggregate>(1)
            .AsQueryable()
            .BuildMock();

        fakeBrandService.GetQueries().Returns(brandData);
        fakeAgentService.GetQueries().Returns(agentData);
        fakePlayerAggregateService.GetQueries().Returns(playerAggregateData);
        fakePlayerBonusAggregateService.GetQueries().Returns(playerBonusAggregateData);
        fakePlayerGameRebateAggregateService.GetQueries().Returns(playerGameAggregateData);
        fakeAgentDividendService.GetQueries().Returns(negativeDividends.AsQueryable().BuildMock());

        var (dividends, _) = await sut.GetAgentDividendsAsync(brandData.First(), 0, default);

        var (sendDividends, noSendDividends) = await sut.GetNoSendDividendsAsync(dividends, default);

        Assert.IsTrue(sendDividends.Count == 5);

        var agentDividendAmount = sendDividends.Where(x => x.AgentId == childAgent2.Id).Sum(x => x.AgentDividendAmount);

        Assert.IsTrue(agentDividendAmount == 20);

        foreach (var dividend in noSendDividends)
        {
            Assert.IsTrue(dividend.SentOn != null);
        }
    }

    [Test]
    public async Task Can_Get_Agent_Dividends_When_Changed_Up_Layer_Agents()
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

        var chiefAgent1 = CreateAgentFixture(brand, 80, null).Create<Agent>();
        var chiefAgent2 = CreateAgentFixture(brand, 90, null).Create<Agent>();

        var childAgent1 = CreateAgentFixture(brand, 20, chiefAgent1).Create<Agent>();
        var childAgent2 = CreateAgentFixture(brand, 50, chiefAgent2).Create<Agent>();

        var agentData = new Agent[]
            {
                chiefAgent1,
                chiefAgent2,
                childAgent1,
                childAgent2
            }
            .AsQueryable()
            .BuildMock();

        var player1 = CreatePlayerFixture(brand, childAgent1).Create<Player>();

        var player2 = CreatePlayerFixture(brand, childAgent2).Create<Player>();

        var playerAggregateData = new PlayerAggregate[]
            {
                CreatePlayerAggregateFixture(brand,
                        childAgent1,
                        30,
                        player1,
                        from,
                        $$"""
                          [
                          {"AgentId":"{{childAgent1.Id}}","ParentAgentId":"{{chiefAgent1.Id}}","DividendPercentage":30},
                          {"AgentId":"{{chiefAgent1.Id}}","ParentAgentId":null,"DividendPercentage":90}]
                          """)
                    .Create<PlayerAggregate>(),
                CreatePlayerAggregateFixture(brand,
                        childAgent1,
                        50,
                        player1,
                        from,
                        $$"""
                          [
                          {"AgentId":"{{childAgent1.Id}}","ParentAgentId":"{{chiefAgent2.Id}}","DividendPercentage":50},
                          {"AgentId":"{{chiefAgent2.Id}}","ParentAgentId":null,"DividendPercentage":90}]
                          """)
                    .Create<PlayerAggregate>(),
                CreatePlayerAggregateFixture(brand,
                        childAgent2,
                        30,
                        player2,
                        from,
                        $$"""
                          [
                          {"AgentId":"{{childAgent2.Id}}","ParentAgentId":"{{chiefAgent1.Id}}","DividendPercentage":30},
                          {"AgentId":"{{chiefAgent1.Id}}","ParentAgentId":null,"DividendPercentage":90}]
                          """)
                    .Create<PlayerAggregate>(),
                CreatePlayerAggregateFixture(brand,
                        childAgent2,
                        50,
                        player2,
                        from,
                        $$"""
                          [
                          {"AgentId":"{{childAgent2.Id}}","ParentAgentId":"{{chiefAgent2.Id}}","DividendPercentage":50},
                          {"AgentId":"{{chiefAgent2.Id}}","ParentAgentId":null,"DividendPercentage":90}]
                          """)
                    .Create<PlayerAggregate>()

            }.AsQueryable()
            .BuildMock();

        var playerBonusAggregateData = new PlayerBonusAggregate[]
            {
                CreatePlayerBonusAggregateFixture(brand, childAgent1, player1, from).Create<PlayerBonusAggregate>(),
                CreatePlayerBonusAggregateFixture(brand, childAgent2, player2, from).Create<PlayerBonusAggregate>()
            }
            .AsQueryable()
            .BuildMock();

        var playerGameAggregateData = new PlayerGameRebateAggregate[]
            {
                CreatePlayerGameAggregateFixture(brand, childAgent1, player1, from).Create<PlayerGameRebateAggregate>(),
                CreatePlayerGameAggregateFixture(brand, childAgent2, player2, from).Create<PlayerGameRebateAggregate>()
            }
            .AsQueryable()
            .BuildMock();

        fakeBrandService.GetQueries().Returns(brandData);
        fakeAgentService.GetQueries().Returns(agentData);
        fakePlayerAggregateService.GetQueries().Returns(playerAggregateData);
        fakePlayerBonusAggregateService.GetQueries().Returns(playerBonusAggregateData);
        fakePlayerGameRebateAggregateService.GetQueries().Returns(playerGameAggregateData);
        fakeAgentDividendService.GetQueries().Returns(Array.Empty<AgentDividend>().AsQueryable().BuildMock());

        var (dividends, _) = await sut.GetAgentDividendsAsync(brandData.First(), 0, default);

        var (sendDividends, noSendDividends) = await sut.GetNoSendDividendsAsync(dividends, default);

        Assert.IsTrue(sendDividends.Count == 6);

        foreach (var dividend in sendDividends)
        {
            var agentTotalDividendAmount = sendDividends.SelectMany(x => x.AgentDividendMultipleLayerModels)
                .Where(x => x.AgentId == dividend.AgentId && x.AgentDividendPercentage == dividend.AgentDividendPercentage)
                .Sum(x => x.Dividend);

            Assert.IsTrue(dividend.AgentDividendAmount == agentTotalDividendAmount - (dividend.AgentAdjustAmount - dividend.NoIncludeNetProfitAgentAdjustAmount));
        }
    }

    private Fixture CreateBrandFixture(DateTimeOffset from, DateTimeOffset to)
    {
        var fixture = new Fixture();
        fixture.Customize<Brand>(u => u
            .With(s => s.AutoSentAgentBonus, true)
            .With(s => s.AgentDividendType, (short)AgentDividendType.OneLayer)
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

    private Fixture CreateAgentFixture(Brand brand,decimal percentage, Agent? parentAgent)
    {
        var fixture = new Fixture();
        fixture.Customize<Agent>(u => u
            .With(s => s.Brand, brand)
            .With(s => s.ParentAgentId, parentAgent?.Id)
            .With(s => s.AgentWallets, new AgentWallet[] { })
            .With(s => s.Players, new Player[] { })
            .With(s => s.AgentDividendConditions, new AgentDividendCondition[] { })
            .With(s => s.InverseParentAgent, new Agent[] { })
            .With(s => s.BrandId, brand.Id)
            .With(s => s.DividendPercentage, percentage));
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
        );
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerAggregateFixture(Brand brand, Agent? agent,decimal dividendPercentage, Player player, DateTimeOffset reportDate, string? jsonString)
    {
        var fixture = new Fixture();
        var percentageJson =
            "[{\"AgentId\":\"" + agent!.Id + "\",\"ParentAgentId\":null,\"DividendPercentage\":" +
            agent.DividendPercentage + "}]";
        fixture.Customize<PlayerAggregate>(u => u
            .With(s => s.ReportDate, reportDate.UtcDateTime)
            .With(s => s.Hour, 0)
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentId, agent.Id)
            .With(s => s.PlayerCode, player.PlayerCode)
            .With(s => s.WinLossAmount, 100)
            .With(s => s.RebateAmount, 0)
            .With(s => s.PromotionAmount, 0)
            .With(s => s.AgentAdjustAmount, 0)
            .With(s => s.NoIncludeNetProfitAgentAdjustAmount, 0)
            .With(s => s.BonusAdjustAmount, 0)
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0)
            .With(s => s.AgentDividendPercentageTreeData, jsonString ?? percentageJson)
            .With(s => s.AgentDividendPercentage, dividendPercentage));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerBonusAggregateFixture(Brand brand, Agent? agent, Player player,
        DateTimeOffset reportDate)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerBonusAggregate>(u => u
            .With(s => s.ReportDate, reportDate.UtcDateTime)
            .With(s => s.Hour, 0)
            .With(s => s.BrandId, brand.Id)
            .With(s => s.AgentId, agent?.Id)
            .With(s => s.BirthdayAmount, 0)
            .With(s => s.SeasonAmount, 0)
            .With(s => s.UpgradeAmount, 0)
            .With(s => s.PlayerCode, player.PlayerCode));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreatePlayerGameAggregateFixture(Brand brand, Agent? agent, Player player,
        DateTimeOffset reportDate)
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