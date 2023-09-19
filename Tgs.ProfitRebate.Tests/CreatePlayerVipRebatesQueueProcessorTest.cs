using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.Notification.Client;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class CreatePlayerVipRebatesQueueProcessorTest
{
    [Test]
    [AutoData]
    public async Task Can_Create_Vip_Player_Rebate(
        PlayerRebateQueueModel playerRebateQueue,
        PlayerRebate playerRebate)
    {
        var fakePlayerRebateTaskQueue = Substitute.For<IPlayerRebateTaskQueue>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeNotificationClient = Substitute.For<INotificationClient>();
        var fakePlayerRebateService = Substitute.For<ITgsService<PlayerRebate>>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerRebateFactory>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerRebateTaskQueueProcessor(
            fakePlayerRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakeNotificationClient,
            fakePlayerRebateService,
            fakeTransactionService,
            fakeLogger);

        _ = fakePlayerRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(playerRebateQueue);

        _ = fakePlayerRebateService.GetQueries()
            .Returns(Array.Empty<PlayerRebate>().AsQueryable().BuildMock());

        _ = fakePlayerRebateFactory
            .CreateVipPlayerRebateAsync(Arg.Any<VipPlayerRebateModel>(), Arg.Any<DateTimeOffset>())
            .Returns(playerRebate);

        _ = fakeTransactionService.GetQueries()
            .Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        await fakePlayerRebateFactory.Received(1)
            .CreateVipPlayerRebateAsync(Arg.Any<VipPlayerRebateModel>(), Arg.Any<DateTimeOffset>());
        await fakePlayerRebateFactory.Received(1)
            .CreatePlayerBucketsAsync(Arg.Any<CreatePlayerBucketRequest>());
        await fakeCashServiceClient.Received(1)
            .RebateAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    [AutoData]
    public async Task Can_Not_Create_Vip_Player_Rebate_When_Rebate_Existed(
        PlayerRebateQueueModel playerRebateQueue)
    {
        var fakePlayerRebateTaskQueue = Substitute.For<IPlayerRebateTaskQueue>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakeNotificationClient = Substitute.For<INotificationClient>();
        var fakePlayerRebateService = Substitute.For<ITgsService<PlayerRebate>>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerRebateFactory>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerRebateTaskQueueProcessor(
            fakePlayerRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakeNotificationClient,
            fakePlayerRebateService,
            fakeTransactionService,
            fakeLogger);

        var playerRebates = new PlayerRebate[]
        {
            new()
            {
                BrandId = playerRebateQueue.BrandId,
                PlayerCode = playerRebateQueue.PlayerCode,
                ReportDate = playerRebateQueue.ReportDate.UtcDateTime,
                VipCardGameRebatePercentage = playerRebateQueue.VipCardGameRebatePercentage,
                VipLiveDealerRebatePercentage = playerRebateQueue.VipLiveDealerRebatePercentage,
                VipLotteryRebatePercentage = playerRebateQueue.VipLotteryRebatePercentage,
                VipSlotRebatePercentage = playerRebateQueue.VipSlotRebatePercentage,
                VipSportsRebatePercentage = playerRebateQueue.VipSportsRebatePercentage,
                VipVirtualSportsRebatePercentage = playerRebateQueue.VipVirtualSportsRebatePercentage
            }
        };

        _ = fakePlayerRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(playerRebateQueue);

        _ = fakePlayerRebateService.GetQueries()
            .Returns(playerRebates.AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        await fakePlayerRebateFactory.Received(0)
            .CreateVipPlayerRebateAsync(Arg.Any<VipPlayerRebateModel>(), Arg.Any<DateTimeOffset>());
        await fakePlayerRebateFactory.Received(0)
            .CreatePlayerBucketsAsync(Arg.Any<CreatePlayerBucketRequest>());
        await fakeCashServiceClient.Received(0)
            .RebateAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>());
    }
}