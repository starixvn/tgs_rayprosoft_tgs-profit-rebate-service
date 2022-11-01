using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Tests
{
    [TestFixture]
    public class CreateAgentDividendFactoryTest
    {
        [Test]
        public Task Can_Get_Interval()
        {
            var fakeAgentDividendService = Substitute.For<ITgsService<AgentDividend>>();
            var fakeAgentService = Substitute.For<ITgsService<Agent>>();
            var fakeAgentWalletRepository = Substitute.For<IAgentWalletRepository>();
            var fakeBrandService = Substitute.For<ITgsService<Brand>>();
            var fakePlayerAggregateService = Substitute.For<ITgsService<PlayerAggregate>>();
            var fakePlayerBonusAggregateService = Substitute.For<ITgsService<PlayerBonusAggregate>>();
            var fakePlayerGameRebateAggregateService = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();

            var sut = new AgentDividendFactory(fakeAgentService, fakeBrandService, fakeAgentDividendService, fakePlayerAggregateService, fakePlayerGameRebateAggregateService, fakePlayerBonusAggregateService, fakeAgentWalletRepository);

            var type = SettlementIntervalType.Daily;
            var startDate = DateTimeOffset.UtcNow;

            var (start,end) =  sut.GetInterval(type, startDate,null);

            Assert.IsTrue(startDate.AddDays(1) == start);
            Assert.IsTrue(startDate.AddDays(1) == end);
            return Task.CompletedTask;
        }

        private static Fixture CreateBrandFixture()
        {
            var fixture = new Fixture();
            fixture.Customize<Brand>(u => u
                .With(s => s.Licensee, new Licensee())
                .With(s => s.Players, new Player[] { }));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }
    }
}