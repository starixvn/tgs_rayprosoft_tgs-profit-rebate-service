using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class CreateAgentDividendsHandlerTest
{
	[Test]
	public async Task Can_Get_Settlement_Interval_Brands()
	{
		var fakeAgentDividendTaskQueue = Substitute.For<IAgentDividendTaskQueue>();

		var sut = new CreateAgentDividendsHandler(fakeAgentDividendTaskQueue);

		var request = new CreateAgentDividendsRequest { BrandCode = null! };

		_ = await sut.Handle(request, default);

		_ = fakeAgentDividendTaskQueue.Received(1)
			.QueueAgentDividendAsync(Arg.Any<AgentDividendQueueModel>());
	}
}
