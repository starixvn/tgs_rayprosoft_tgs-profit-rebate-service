using AutoFixture;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.ProfitRebate.Core;
using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Core.Workers;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using TGS.Core.Infrastructures.Extensions;

namespace Tgs.ProfitRebate.Tests;

[TestFixture]
public class CreatePlayerAllAgentRebatesQueueProcessorTest
{
	[Test]
	public async Task Can_Save_All_Agent_Rebates()
	{
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakePlayerAllAgentRebateService = Substitute.For<ITgsService<PlayerAllAgentRebateRecord>>();
        var fakePlayerAllAgentRebateContributorService = Substitute.For<ITgsService<PlayerAllAgentRebateContributor>>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerAllAgentRebateTaskQueueProcessor(
            fakePlayerAllAgentRebateTaskQueue, 
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakePlayerServiceClient,
            fakePlayerAllAgentRebateService,
            fakePlayerAllAgentRebateContributorService,
            fakeTransactionService,
            fakeLogger);

		var brandId = Guid.NewGuid();
        var reportDate = DateTimeOffset.Parse("2023/01/01 00:00:00+8");
        var parentPlayerCode = "parentPlayerCode";

        var conditions = new BrandAllAgentGameTypeConditionModel[]
        {
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.Sportsbook,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            },
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.VirtualSports,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            }
        };

        var turnoverWinLoss = new PlayerAllAgentTurnoverWinLossModel[]
        {
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode",
                GameType= GameType.Sportsbook,
                Turnover = 100,
                WinLoss= 100,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.ESports,
                Turnover = 10,
                WinLoss= 10,
            }
        };

        var request = new PlayerAllAgentRebateQueueModel
        {
            PlayerCode = parentPlayerCode,
            ReportDate = reportDate,
            Conditions = conditions
        };

        var player = new Dictionary<string, string> { { "DefaultWalletId", Guid.NewGuid().ToString() } }.ToImmutableDictionary();

        _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Any<string>())
            .Returns(player);

        _ = fakePlayerAllAgentRebateService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateRecord>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateContributorService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateContributor>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(request);

        _ = fakePlayerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
			.Returns(turnoverWinLoss);

        _ = fakeTransactionService.GetQueries()
            .Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

		await fakePlayerRebateFactory.Received(1)
            .CreateAllAgentPlayerRebatesAsync(Arg.Any<PlayerAllAgentRebateModel[]>());
	}

    [Test]
    public async Task Can_Save_2_Types_All_Agent_Rebates()
    {
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakePlayerAllAgentRebateService = Substitute.For<ITgsService<PlayerAllAgentRebateRecord>>();
        var fakePlayerAllAgentRebateContributorService = Substitute.For<ITgsService<PlayerAllAgentRebateContributor>>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerAllAgentRebateTaskQueueProcessor(
            fakePlayerAllAgentRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakePlayerServiceClient,
            fakePlayerAllAgentRebateService,
            fakePlayerAllAgentRebateContributorService,
            fakeTransactionService,
            fakeLogger);

        var brandId = Guid.NewGuid();
        var reportDate = DateTimeOffset.Parse("2023/01/01 00:00:00+8");
        var parentPlayerCode = "parentPlayerCode";

        var conditions = new BrandAllAgentGameTypeConditionModel[]
        {
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.Sportsbook,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            },
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.VirtualSports,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 5,
                RebatePercentage = 10
            }
        };

        var turnoverWinLoss = new PlayerAllAgentTurnoverWinLossModel[]
        {
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode",
                GameType= GameType.Sportsbook,
                Turnover = 100,
                WinLoss= 100,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.ESports,
                Turnover = 10,
                WinLoss= 10,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.VirtualSports,
                Turnover = 10,
                WinLoss= 10,
            }
        };

        var request = new PlayerAllAgentRebateQueueModel
        {
            PlayerCode = parentPlayerCode,
            ReportDate = reportDate,
            Conditions = conditions
        };

        var player = new Dictionary<string, string> { { "DefaultWalletId", Guid.NewGuid().ToString() } }.ToImmutableDictionary();

        _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Any<string>())
            .Returns(player);

        _ = fakePlayerAllAgentRebateService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateRecord>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateContributorService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateContributor>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(request);

        _ = fakePlayerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(turnoverWinLoss);

        _ = fakeTransactionService.GetQueries()
            .Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        await fakePlayerRebateFactory.Received(1)
            .CreateAllAgentPlayerRebatesAsync(Arg.Any<PlayerAllAgentRebateModel[]>());
    }

    [Test]
    public async Task Can_Save_All_Agent_Rebates_And_Right_Record()
    {
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakePlayerAllAgentRebateService = Substitute.For<ITgsService<PlayerAllAgentRebateRecord>>();
        var fakePlayerAllAgentRebateContributorService = Substitute.For<ITgsService<PlayerAllAgentRebateContributor>>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerAllAgentRebateTaskQueueProcessor(
            fakePlayerAllAgentRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakePlayerServiceClient,
            fakePlayerAllAgentRebateService,
            fakePlayerAllAgentRebateContributorService,
            fakeTransactionService,
            fakeLogger);

        var brandId = Guid.NewGuid();
        var reportDate = DateTimeOffset.Parse("2023/01/01 00:00:00+8");
        var parentPlayerCode = "parentPlayerCode";

        var conditions = new BrandAllAgentGameTypeConditionModel[]
        {
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.Sportsbook,
                AllAgentRebateType = AllAgentRebateType.ValidBet,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            }
        };

        var turnoverWinLoss = new PlayerAllAgentTurnoverWinLossModel[]
        {
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode",
                GameType= GameType.Sportsbook,
                Turnover = 100,
                WinLoss= 100,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.Sportsbook,
                Turnover = 10,
                WinLoss= 10,
            }
        };

        var request = new PlayerAllAgentRebateQueueModel
        {
            PlayerCode = parentPlayerCode,
            ReportDate = reportDate,
            Conditions = conditions
        };

        var player = new Dictionary<string, string> { { "DefaultWalletId", Guid.NewGuid().ToString() } }.ToImmutableDictionary();

        _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Any<string>())
            .Returns(player);

        _ = fakePlayerAllAgentRebateService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateRecord>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateContributorService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateContributor>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(request);

        _ = fakePlayerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(turnoverWinLoss);

        _ = fakeTransactionService.GetQueries()
            .Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        await fakePlayerRebateFactory.Received(1)
            .CreateAllAgentPlayerRebatesAsync(Arg.Is<PlayerAllAgentRebateModel[]>(x =>
                x.Any(a =>
                    a.BrandId == brandId &&
                    a.PlayerCode == parentPlayerCode &&
                    a.ReportDate == reportDate.UtcDateTime &&
                    a.AllAgentGameType == AllAgentGameType.Sportsbook &&
                    a.AllAgentRebateType == AllAgentRebateType.ValidBet &&
                    a.Rank == 1 &&
                    a.ActiveNumber == 1 &&
                    a.ConditionAmount == 110 &&
                    a.RebatePercentage == 10 &&
                    a.ActualPlayerNumber == 2 &&
                    a.ActualConditionAmount == 110 &&
                    a.RebateAmount == 11)));
    }

    [Test]
    public async Task Can_Save_All_Agent_Rebate_Contributors_For_Turnover()
    {
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakePlayerAllAgentRebateService = Substitute.For<ITgsService<PlayerAllAgentRebateRecord>>();
        var fakePlayerAllAgentRebateContributorService = Substitute.For<ITgsService<PlayerAllAgentRebateContributor>>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerAllAgentRebateTaskQueueProcessor(
            fakePlayerAllAgentRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakePlayerServiceClient,
            fakePlayerAllAgentRebateService,
            fakePlayerAllAgentRebateContributorService,
            fakeTransactionService,
            fakeLogger);

        var brandId = Guid.NewGuid();
        var reportDate = DateTimeOffset.Parse("2023/01/01 00:00:00+8");
        var parentPlayerCode = "parentPlayerCode";

        var conditions = new BrandAllAgentGameTypeConditionModel[]
        {
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.Sportsbook,
                AllAgentRebateType = AllAgentRebateType.ValidBet,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            }
        };

        var turnoverWinLoss = new PlayerAllAgentTurnoverWinLossModel[]
        {
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode",
                GameType= GameType.Sportsbook,
                Turnover = 100,
                WinLoss= 100,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.Sportsbook,
                Turnover = 10,
                WinLoss= 10,
            }
        };

        var request = new PlayerAllAgentRebateQueueModel
        {
            PlayerCode = parentPlayerCode,
            ReportDate = reportDate,
            Conditions = conditions
        };

        var player = new Dictionary<string, string> { { "DefaultWalletId", Guid.NewGuid().ToString() } }.ToImmutableDictionary();

        _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Any<string>())
            .Returns(player);

        _ = fakePlayerAllAgentRebateService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateRecord>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateContributorService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateContributor>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(request);

        _ = fakePlayerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(turnoverWinLoss);

        _ = fakeTransactionService.GetQueries()
            .Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        var rebates = new (decimal SportsTurnover, decimal SportsTurnoverRebate)[] { (100M, 10M), (10M, 1M) };
        await fakePlayerRebateFactory.Received(2)
            .CreateAllAgentPlayerRebateContributorAsync(Arg.Is<PlayerAllAgentRebateContributorModel>(x =>
                rebates.Any(r => r.SportsTurnover == x.SportsTurnover && r.SportsTurnoverRebate == x.SportsTurnoverRebate)));
    }

    [Test]
    public async Task Can_Save_All_Agent_Rebate_Contributors_For_WinLoss()
    {
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakePlayerAllAgentRebateService = Substitute.For<ITgsService<PlayerAllAgentRebateRecord>>();
        var fakePlayerAllAgentRebateContributorService = Substitute.For<ITgsService<PlayerAllAgentRebateContributor>>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerAllAgentRebateTaskQueueProcessor(
            fakePlayerAllAgentRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakePlayerServiceClient,
            fakePlayerAllAgentRebateService,
            fakePlayerAllAgentRebateContributorService,
            fakeTransactionService,
            fakeLogger);

        var brandId = Guid.NewGuid();
        var reportDate = DateTimeOffset.Parse("2023/01/01 00:00:00+8");
        var parentPlayerCode = "parentPlayerCode";

        var conditions = new BrandAllAgentGameTypeConditionModel[]
        {
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.Sportsbook,
                AllAgentRebateType = AllAgentRebateType.WinLoss,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            }
        };

        var turnoverWinLoss = new PlayerAllAgentTurnoverWinLossModel[]
        {
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode",
                GameType= GameType.Sportsbook,
                Turnover = 100,
                WinLoss= 100,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.Sportsbook,
                Turnover = 10,
                WinLoss= 10,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode2",
                GameType= GameType.Sportsbook,
                Turnover = 10,
                WinLoss= 10,
            }
        };

        var request = new PlayerAllAgentRebateQueueModel
        {
            PlayerCode = parentPlayerCode,
            ReportDate = reportDate,
            Conditions = conditions
        };

        var player = new Dictionary<string, string> { { "DefaultWalletId", Guid.NewGuid().ToString() } }.ToImmutableDictionary();

        _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Any<string>())
            .Returns(player);

        _ = fakePlayerAllAgentRebateService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateRecord>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateContributorService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateContributor>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(request);

        _ = fakePlayerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(turnoverWinLoss);

        _ = fakeTransactionService.GetQueries()
            .Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        var rebates = new (decimal SportsWinLoss, decimal SportsWinLossRebate)[] { (100M, 10M), (10M, 1M) };

        await fakePlayerRebateFactory.Received(3)
            .CreateAllAgentPlayerRebateContributorAsync(Arg.Is<PlayerAllAgentRebateContributorModel>(x =>
                rebates.Any(r => r.SportsWinLoss == x.SportsWinLoss && r.SportsWinLossRebate == x.SportsWinLossRebate)));
    }

    [Test]
    public async Task Can_Send_2_Game_Types_All_Agent_Rebates()
    {
        var fakePlayerAllAgentRebateTaskQueue = Substitute.For<IPlayerAllAgentRebateTaskQueue>();
        var fakePlayerRebateFactory = Substitute.For<IPlayerAllAgentRebateFactory>();
        var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
        var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
        var fakePlayerAllAgentRebateService = Substitute.For<ITgsService<PlayerAllAgentRebateRecord>>();
        var fakePlayerAllAgentRebateContributorService = Substitute.For<ITgsService<PlayerAllAgentRebateContributor>>();
        var fakeTransactionService = Substitute.For<ITgsService<Transaction>>();
        var fakeLogger = Substitute.For<ILogger<PlayerAllAgentRebateTaskQueueProcessor>>();

        var sut = new PlayerAllAgentRebateTaskQueueProcessor(
            fakePlayerAllAgentRebateTaskQueue,
            fakePlayerRebateFactory,
            fakeCashServiceClient,
            fakePlayerServiceClient,
            fakePlayerAllAgentRebateService,
            fakePlayerAllAgentRebateContributorService,
            fakeTransactionService,
            fakeLogger);

        var brandId = Guid.NewGuid();
        var reportDate = DateTimeOffset.Parse("2023/01/01 00:00:00+8");
        var parentPlayerCode = "parentPlayerCode";

        var conditions = new BrandAllAgentGameTypeConditionModel[]
        {
            new BrandAllAgentGameTypeConditionModel
            {
                BrandId= brandId,
                GameTypeSwitch = true,
                HasValidBet = true,
                HasWinLoss= true,
                AllAgentGameType = AllAgentGameType.Sportsbook,
                AllAgentRebateType = AllAgentRebateType.ValidBet,
                Rank = 1,
                ActiveNumber= 1,
                NetProfit= 110,
                RebatePercentage = 10
            }
        };

        var turnoverWinLoss = new PlayerAllAgentTurnoverWinLossModel[]
        {
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode",
                GameType= GameType.Sportsbook,
                Turnover = 100,
                WinLoss= 100,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.Sportsbook,
                Turnover = 10,
                WinLoss= 10,
            },
            new PlayerAllAgentTurnoverWinLossModel
            {
                BrandId = brandId,
                PlayerCode = "parentPlayerCode",
                ChildPlayerCode = "playerCode1",
                GameType= GameType.VirtualSports,
                Turnover = 10,
                WinLoss= 10,
            }
        };

        var request = new PlayerAllAgentRebateQueueModel
        {
            PlayerCode = parentPlayerCode,
            ReportDate = reportDate,
            Conditions = conditions
        };

        var contributors = new PlayerAllAgentRebateContributor[]
            {
                CreatePlayerAllAgentRebateContributorFixture(brandId, reportDate, parentPlayerCode, "player")
                    .Create<PlayerAllAgentRebateContributor>(),
                CreatePlayerAllAgentRebateContributorFixture(brandId, reportDate, parentPlayerCode, "player1")
                    .Create<PlayerAllAgentRebateContributor>()
            }
            .AsQueryable()
            .BuildMock();

        var player = new Dictionary<string, string> { { "DefaultWalletId", Guid.NewGuid().ToString() } }.ToImmutableDictionary();

        _ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Any<string>())
            .Returns(player);

        _ = fakePlayerAllAgentRebateService.GetQueries().Returns(Array.Empty<PlayerAllAgentRebateRecord>().AsQueryable().BuildMock());

        _ = fakePlayerAllAgentRebateContributorService.GetQueries().Returns(contributors);

        _ = fakePlayerAllAgentRebateTaskQueue.DequeueAsync(Arg.Any<CancellationToken>()).Returns(request);

        _ = fakePlayerRebateFactory.GetPlayerAllAgentTurnoverWinLossAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>())
            .Returns(turnoverWinLoss);

        _ = fakeTransactionService.GetQueries().Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

        await sut.ProcessQueue(default);

        await fakePlayerRebateFactory.Received(2)
            .CreateAllAgentPlayerRebateContributorAsync(Arg.Any<PlayerAllAgentRebateContributorModel>());
    }

    private static Fixture CreatePlayerAllAgentRebateContributorFixture(
        Guid brandId,
        DateTimeOffset reportDate,
        string playerCode,
        string childPlayerCode)
    {
        var fixture = new Fixture();
        fixture.Customize<PlayerAllAgentRebateContributor>(u
            => u.With(x => x.BrandId, brandId)
                .With(x => x.ReportDate, reportDate.UtcDateTime)
                .With(x => x.PlayerCode, playerCode)
                .With(x => x.ChildPlayerCode, childPlayerCode)
                .With(x => x.SentOn, (DateTimeOffset?)null));

        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

}
