using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Tgs.ProfitRebate.Core.Commands;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Tests
{
    [TestFixture]
    public class CreateAgentDividendsHandlerTest
    {
        [Test]
        public async Task Can_Get_Settlement_Interval_Brands()
        {
            var fakeAgentDividendTaskQueue = Substitute.For<IAgentDividendTaskQueue>();

            var sut = new CreateAgentDividendsHandler(fakeAgentDividendTaskQueue);

            var request = new CreateAgentDividendsRequest { BrandCode = null! };

            var now = DateTimeOffset.Now.ToOffset(new(8, 0, 0));
            var queryDate = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, new(8, 0, 0));

            await sut.Handle(request, default);

            _ = fakeAgentDividendTaskQueue.Received(1)
                .QueueAgentDividendAsync(Arg.Any<AgentDividendQueueModel>());
        }
    }
}