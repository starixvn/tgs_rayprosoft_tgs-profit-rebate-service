using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class AgentDividendDistributionHandlerTest
{
    [Test]
    public async Task Send_Agent_Dividends()
    {
        var fakeAgentService = Substitute.For<ITgsService<Agent>>();
        var fakeAgentDividendService = Substitute.For<ITgsService<AgentDividend>>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeAgentDividendFactory = Substitute.For<IAgentDividendFactory>();

        var sut = new AgentDividendDistributionHandler(fakeCashServiceClient, fakeAgentService, fakeAgentDividendService, fakeAgentDividendFactory);

        var agentId = Guid.NewGuid();

        var request = new AgentDividendDistributionRequest(agentId);

        var agents = CreateAgentFixture(agentId)
            .CreateMany<Agent>(1).AsQueryable().BuildMock();

        var agentDividends = new AgentDividend[]
        {
            CreateAgentDividendFixture(agentId, 10).Create<AgentDividend>(),
            CreateAgentDividendFixture(agentId, 100).Create<AgentDividend>()
        }.AsQueryable().BuildMock();

        fakeAgentDividendService.GetQueries().Returns(agentDividends);
        fakeAgentService.GetQueries().Returns(agents);

        await sut.Handle(request, default);

        _ = fakeCashServiceClient.ReceivedWithAnyArgs(2)
            .AgentDividendAsync(default, 0, null, null, default);
    }

    private Fixture CreateAgentFixture(Guid agentId)
    {
        var fixture = new Fixture();
        fixture.Customize<Agent>(u => u
            .With(s => s.Brand, new Brand() { })
            .With(s => s.AgentWallets, new AgentWallet[] { })
            .With(s => s.Players, new Player[] { })
            .With(s => s.AgentDividendConditions, new AgentDividendCondition[] { })
            .With(s => s.InverseParentAgent, new Agent[] { })
            .With(s => s.Id, agentId));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateAgentDividendFixture(Guid agentId, decimal agentDividendAmount)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividend>(u => u
            .With(s => s.SentOn, (DateTime?)null)
            .With(s => s.AgentId, agentId)
            .With(s => s.AgentDividendAmount, agentDividendAmount));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}