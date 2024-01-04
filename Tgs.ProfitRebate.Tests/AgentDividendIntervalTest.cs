using System;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class AgentDividendIntervalTest
{
    [Test]
    public void Can_Get_Daily_Interval()
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
            fakeAgentWalletRepository,
            NullLogger<AgentDividendFactory>.Instance);

        const SettlementIntervalType type = SettlementIntervalType.Daily;
        var startDate = new DateTimeOffset(2022, 10, 01, 0, 0, 0, new(8, 0, 0));

        var (start, end) = sut.GetInterval(type, startDate, null);

        Assert.IsTrue(startDate.AddDays(1) == start);
        Assert.IsTrue(startDate.AddDays(1) == end);
    }

    [Test]
    public void Can_Get_Weekly_Interval()
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
            fakeAgentWalletRepository,
            NullLogger<AgentDividendFactory>.Instance);

        const SettlementIntervalType type = SettlementIntervalType.Weekly;
        var startDate = new DateTimeOffset(2022, 10, 02, 0, 0, 0, new(8, 0, 0));

        var (start, end) = sut.GetInterval(type, startDate, null);

        Assert.IsTrue(startDate.AddDays(1) == start);
        Assert.IsTrue(startDate.AddDays(7) == end);
    }

    [Test]
    public void Can_Get_Monthly_Interval()
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
            fakeAgentWalletRepository,
            NullLogger<AgentDividendFactory>.Instance);

        const SettlementIntervalType type = SettlementIntervalType.Monthly;
        var startDate = new DateTimeOffset(2022, 10, 31, 0, 0, 0, new(8, 0, 0));

        var (start, end) = sut.GetInterval(type, startDate, null);

        Assert.IsTrue(new DateTimeOffset(2022, 11, 1, 0, 0, 0, new(8, 0, 0)) == start);
        Assert.IsTrue(new DateTimeOffset(2022, 11, 30, 0, 0, 0, new(8, 0, 0)) == end);
    }

    [Test]
    public void Can_Get_Monthly_Interval_By_Day()
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
            fakeAgentWalletRepository,
            NullLogger<AgentDividendFactory>.Instance);

        var type = SettlementIntervalType.Monthly;
        var startDate = new DateTimeOffset(2022, 11, 2, 0, 0, 0, new(8, 0, 0));

        var (start, end) = sut.GetInterval(type, startDate, 3);

        Assert.IsTrue(new DateTimeOffset(2022, 11, 3, 0, 0, 0, new(8, 0, 0)) == start);
        Assert.IsTrue(new DateTimeOffset(2022, 12, 3, 0, 0, 0, new(8, 0, 0)) == end);
    }

    [Test]
    public void Can_Not_Get_Monthly_Interval()
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
            fakeAgentWalletRepository,
            NullLogger<AgentDividendFactory>.Instance);

        var startDate = new DateTimeOffset(2022, 10, 31, 0, 0, 0, new(8, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.GetInterval((SettlementIntervalType)4, startDate, null));
    }
}