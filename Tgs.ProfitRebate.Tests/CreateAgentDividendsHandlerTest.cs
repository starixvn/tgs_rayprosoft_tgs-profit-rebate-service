using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Repository.Models;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Tests
{
    [TestFixture]
    public class CreateAgentDividendsHandlerTest
    {
        [Test]
        public async Task Can_Get_Settlement_Interval_Brands()
        {
            var fakeAgentDividendFactory = Substitute.For<IAgentDividendFactory>();
            var fakeAgentDividendRepository = Substitute.For<ITgsService<AgentDividend>>();

            var sut = new CreateAgentDividendsHandler(fakeAgentDividendRepository, fakeAgentDividendFactory);

            var request = new CreateAgentDividendsRequest { BrandCode = null };

            var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));
            var queryDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, new(8, 0, 0));

            await sut.Handle(request, default);

            _ = fakeAgentDividendFactory.Received(1)
                .GetSettlementIntervalBrands(
                    Arg.Any<string>(),
                    Arg.Is(queryDate));
        }

        [Test]
        public async Task Can_Create_Agent_Dividends()
        {
            var fakeAgentDividendFactory = Substitute.For<IAgentDividendFactory>();
            var fakeAgentDividendRepository = Substitute.For<ITgsService<AgentDividend>>();

            var sut = new CreateAgentDividendsHandler(fakeAgentDividendRepository, fakeAgentDividendFactory);

            var request = new CreateAgentDividendsRequest { BrandCode = null };

            var brands = GetBrands(1);

            fakeAgentDividendFactory.GetSettlementIntervalBrands(null, Arg.Any<DateTimeOffset>())
                .Returns(brands);

            var brand = await brands.FirstAsync();

            var agentDividends = CreateAgentDividendFixture(brand.First().Id)
                .CreateMany<AgentDividend>(1)
                .AsQueryable()
                .BuildMock().Object;

            fakeAgentDividendRepository.GetQueries()
                .Returns(agentDividends);

            fakeAgentDividendFactory.GetAgentDividends(Arg.Any<Brand>(), Arg.Any<int>()).Returns(Task.FromResult((
                new List<CreateAgentDividendModel>(), new List<CreateAgentDividendModel>(), new List<AgentDividend>(),
                0)));

            await sut.Handle(request, default);

            await fakeAgentDividendFactory.Received(1)
                .SaveAgentDividendData(Arg.Any<Brand[]>(), Arg.Any<List<CreateAgentDividendModel>>(),
                    Arg.Any<List<CreateAgentDividendModel>>(), Arg.Any<List<AgentDividend>>());
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

        private static Fixture CreateAgentDividendFixture(Guid brandId)
        {
            var fixture = new Fixture();
            fixture.Customize<AgentDividend>(u => u
                .With(s => s.BrandId, brandId)
                .With(s => s.AgentDividendMultipleLayers, new AgentDividendMultipleLayer[] { }));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private async IAsyncEnumerable<Brand[]> GetBrands(int totalNumber)
        {
            var enumerator = CreateBrandFixture().CreateMany<Brand>(totalNumber).AsQueryable().ToArray();
            yield return enumerator;
        }
    }
}