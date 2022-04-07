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
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Tests
{
    [TestFixture]
    public class CreatePlayerAllAgentRebatesHandlerTest
    {
        [Test]
        public async Task Can_Create_All_Agent_Rebates_With_Not_Match()
        {
            var fakePlayerRebateFactory = Substitute.For<IPlayerRebateFactory>();
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakePlayerRepository = Substitute.For<ITgsService<Player>>();
            var fakePlayerAllAgentRebateRepository = Substitute.For<ITgsService<PlayerAllAgentRebate>>();

            var sut = new CreatePlayerAllAgentRebatesHandler(fakeCashServiceClient, fakePlayerRepository,
                fakePlayerAllAgentRebateRepository, fakePlayerRebateFactory);

            var request = new CreatePlayerAllAgentRebatesRequest(null);

            var fixture = new Fixture();

            var allAgentPlayerTurnoverWinLoss = fixture.CreateMany<PlayerAllAgentTurnoverWinLossModel>(1);
            var brandAllAgentGameTypeConditions = fixture.CreateMany<BrandAllAgentGameTypeConditionModel>(1);

            fakePlayerRebateFactory.GetPlayerTurnoverWinLossAsync(Arg.Any<DateTimeOffset>())
                .Returns(ValueTask.FromResult(allAgentPlayerTurnoverWinLoss.ToArray()));

            fakePlayerRebateFactory.GetBrandAllAgentGameTypeConditionsAsync(Arg.Any<Guid[]>())
                .Returns(ValueTask.FromResult(brandAllAgentGameTypeConditions.ToArray()));

            var players = CreatePlayerFixture("playerCode")
                .CreateMany<Player>(1)
                .AsQueryable()
                .BuildMock().Object;

            fakePlayerRepository.GetQueries()
                .Returns(players);

            var playerAllAgentRebates = fixture.CreateMany<PlayerAllAgentRebate>(0).AsQueryable().BuildMock().Object;
            fakePlayerAllAgentRebateRepository.GetQueries()
                .Returns(playerAllAgentRebates);

            await sut.Handle(request, default);

            await fakePlayerRebateFactory.Received(0)
                .CreateAllAgentPlayerRebatesAsync(Arg.Any<PlayerAllAgentRebateModel[]>(), Arg.Any<DateTimeOffset>());
        }

        [Test]
        public async Task Can_Create_All_Agent_Rebates()
        {
            var fakePlayerRebateFactory = Substitute.For<IPlayerRebateFactory>();
            var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
            var fakePlayerRepository = Substitute.For<ITgsService<Player>>();
            var fakePlayerAllAgentRebateRepository = Substitute.For<ITgsService<PlayerAllAgentRebate>>();

            var sut = new CreatePlayerAllAgentRebatesHandler(fakeCashServiceClient, fakePlayerRepository,
                fakePlayerAllAgentRebateRepository, fakePlayerRebateFactory);

            var request = new CreatePlayerAllAgentRebatesRequest(null);

            var fixture = new Fixture();

            fixture.Customize<PlayerAllAgentTurnoverWinLossModel>(u
                => u.With(x => x.GameType, GameType.Sportsbook));

            var playerAllAgentTurnoverWinLoss = CreatePlayerAllAgentTurnoverWinLossModelFixture(GameType.Sportsbook)
                .CreateMany<PlayerAllAgentTurnoverWinLossModel>(1);

            var firstPlayerAllAgentTurnoverWinLoss = playerAllAgentTurnoverWinLoss.First();

            var brandAllAgentGameTypeConditions =
                CreateBrandAllAgentGameTypeConditionModelFixture(firstPlayerAllAgentTurnoverWinLoss)
                    .CreateMany<BrandAllAgentGameTypeConditionModel>(1);

            fakePlayerRebateFactory.GetPlayerTurnoverWinLossAsync(Arg.Any<DateTimeOffset>())
                .Returns(ValueTask.FromResult(playerAllAgentTurnoverWinLoss.ToArray()));

            fakePlayerRebateFactory.GetBrandAllAgentGameTypeConditionsAsync(Arg.Any<Guid[]>())
                .Returns(ValueTask.FromResult(brandAllAgentGameTypeConditions.ToArray()));

            var players = CreatePlayerFixture("playerCode")
                .CreateMany<Player>(1)
                .AsQueryable()
                .BuildMock().Object;

            fakePlayerRepository.GetQueries()
                .Returns(players);

            var playerAllAgentRebates = fixture.CreateMany<PlayerAllAgentRebate>(0).AsQueryable().BuildMock().Object;
            fakePlayerAllAgentRebateRepository.GetQueries()
                .Returns(playerAllAgentRebates);

            await sut.Handle(request, default);

            await fakePlayerRebateFactory.Received(1)
                .CreateAllAgentPlayerRebatesAsync(Arg.Any<PlayerAllAgentRebateModel[]>(), Arg.Any<DateTimeOffset>());
            await fakePlayerRebateFactory.Received(1)
                .CreatePlayerBucketsAsync(Arg.Any<CreatePlayerBucketRequest[]>());
        }

        private static Fixture CreatePlayerAllAgentTurnoverWinLossModelFixture(GameType type)
        {
            var fixture = new Fixture();
            fixture.Customize<PlayerAllAgentTurnoverWinLossModel>(u
                => u.With(x => x.ParentPlayerCode, "playerCode")
                    .With(x => x.GameType, type));

            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private static Fixture CreateBrandAllAgentGameTypeConditionModelFixture(
            PlayerAllAgentTurnoverWinLossModel model)
        {
            var fixture = new Fixture();
            fixture.Customize<BrandAllAgentGameTypeConditionModel>(u
                => u.With(x => x.BrandId, model.BrandId)
                    .With(x => x.AllAgentGameType, AllAgentGameType.Sportsbook)
                    .With(x => x.ActiveNumber, model.PlayerNumber)
                    .With(x => x.NetProfit, model.SportsTurnover)
                    .With(x => x.HasValidBet, true)
                    .With(x => x.HasWinLoss, true));

            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        private static Fixture CreatePlayerFixture(string playerCode)
        {
            var fixture = new Fixture();
            fixture.Customize<Player>(u => u
                .With(s => s.PlayerCode, playerCode)
                .With(s => s.PlayerBuckets, new PlayerBucket[] { })
                .With(s => s.Wallets, new Wallet[] { }));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }
    }
}