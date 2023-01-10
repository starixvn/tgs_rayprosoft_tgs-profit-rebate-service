using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Repository;

public interface ITgsRepository
{
	DbSet<AgentDividend> AgentDividends { get; set; }

	DbSet<AgentTransaction> AgentTransactions { get; set; }

	DbSet<AgentWallet> AgentWallets { get; set; }

	DbSet<Agent> Agents { get; set; }

	DbSet<Brand> Brands { get; set; }

	DbSet<Licensee> Licensees { get; set; }

	DbSet<PlayerAggregate> PlayerAggregates { get; set; }

	DbSet<PlayerGameRebateAggregate> PlayerGameRebateAggregates { get; set; }

	DbSet<Player> Players { get; set; }

	DbSet<Reseller> Resellers { get; set; }

	DbSet<RoundAggregate> RoundAggregates { get; set; }

	DbSet<TransactionPromotion> TransactionPromotions { get; set; }

	DbSet<Transaction> Transactions { get; set; }

	DbSet<User> Users { get; set; }

	DbSet<WithdrawForm> WithdrawForms { get; set; }

	DbSet<BrandGame> BrandGames { get; set; }

	DbSet<PlayerParent> PlayerParents { get; set; }

	DbSet<BrandAllAgent> BrandAllAgents { get; set; }

	DbSet<BrandAllAgentGameType> BrandAllAgentGameTypes { get; set; }

	DbSet<BrandAllAgentGameTypeCondition> BrandAllAgentGameTypeConditions { get; set; }

	DbSet<PlayerAllAgentRebateRecord> PlayerAllAgentRebateRecords { get; set; }

    DbSet<PlayerAllAgentRebateContributor> PlayerAllAgentRebateContributors { get; set; }

    DbSet<PlayerBonusAggregate> PlayerBonusAggregates { get; set; }

	IDbContextTransaction? CurrentTransaction { get; }

	DbSet<T> Set<T>() where T : class;

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	IDbConnection GetDbConnection();
}
