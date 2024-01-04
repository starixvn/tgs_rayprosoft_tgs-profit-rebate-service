using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class AgentDividendSaveDataTest
{
    [Test]
    public async Task Can_Get_Brands()
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

        var brandData = CreateBrandFixture()
            .CreateMany<Brand>(1)
            .AsQueryable()
            .BuildMock();

        var brandCode = brandData.First().Code;
        var queryDate = new DateTimeOffset(2022, 10, 2, 0, 0, 0, new(8, 0, 0));

        fakeBrandService.GetQueries().Returns(brandData);

        var response = await sut.GetSettlementIntervalBrandsAsync(brandCode, queryDate).FirstAsync();

        Assert.IsTrue(response.First().Code == brandCode);
    }

    [Test]
    public async Task Can_Not_Get_Brands_When_None_Of_Brand_In_The_Settle_Interval()
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

        var brandData = CreateBrandFixture()
            .CreateMany<Brand>(1)
            .AsQueryable()
            .BuildMock();

        var brandCode = brandData.First().Code;
        var queryDate = new DateTimeOffset(2022, 10, 1, 0, 0, 0, new(8, 0, 0));

        fakeBrandService.GetQueries().Returns(brandData);

        var response = await sut.GetSettlementIntervalBrandsAsync(brandCode, queryDate).FirstAsync();

        Assert.IsTrue(response.Length == 0);
    }

    private static Fixture CreateBrandFixture()
    {
        var fixture = new Fixture();
        fixture.Customize<Brand>(u => u
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
}