using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Repository;

public class TgsRepository : DbContext, ITgsRepository
{
	public IDbContextTransaction? CurrentTransaction =>
		Database.CurrentTransaction;

	public DbSet<Agent> Agents { get; set; } = null!;

	public DbSet<AgentDividend> AgentDividends { get; set; } = null!;

	public DbSet<AgentDividendCondition> AgentDividendConditions { get; set; } = null!;

	public DbSet<AgentDividendMultipleLayer> AgentDividendMultipleLayers { get; set; } = null!;

	public DbSet<AgentTransaction> AgentTransactions { get; set; } = null!;

	public DbSet<AgentWallet> AgentWallets { get; set; } = null!;

	public DbSet<Brand> Brands { get; set; } = null!;

	public DbSet<BrandAgentDividendCondition> BrandAgentDividendConditions { get; set; } = null!;

	public DbSet<BrandAllAgent> BrandAllAgents { get; set; } = null!;

	public DbSet<BrandAllAgentGameType> BrandAllAgentGameTypes { get; set; } = null!;

	public DbSet<BrandAllAgentGameTypeCondition> BrandAllAgentGameTypeConditions { get; set; } = null!;

	public DbSet<BrandGame> BrandGames { get; set; } = null!;

	public DbSet<Currency> Currencies { get; set; } = null!;

	public DbSet<Department> Departments { get; set; } = null!;

	public DbSet<Licensee> Licensees { get; set; } = null!;

	public DbSet<Player> Players { get; set; } = null!;

	public DbSet<PlayerAggregate> PlayerAggregates { get; set; } = null!;

	public DbSet<PlayerAllAgentRebate> PlayerAllAgentRebates { get; set; } = null!;

	public DbSet<PlayerBucket> PlayerBuckets { get; set; } = null!;

	public DbSet<PlayerGameRebateAggregate> PlayerGameRebateAggregates { get; set; } = null!;

	public DbSet<PlayerParent> PlayerParents { get; set; } = null!;

	public DbSet<PlayerRebate> PlayerRebates { get; set; } = null!;

	public DbSet<Promotion> Promotions { get; set; } = null!;

	public DbSet<Reseller> Resellers { get; set; } = null!;

	public DbSet<Role> Roles { get; set; } = null!;

	public DbSet<RoundAggregate> RoundAggregates { get; set; } = null!;

	public DbSet<Transaction> Transactions { get; set; } = null!;

	public DbSet<TransactionPromotion> TransactionPromotions { get; set; } = null!;

	public DbSet<User> Users { get; set; } = null!;

	public DbSet<VipLevel> VipLevels { get; set; } = null!;

	public DbSet<Wallet> Wallets { get; set; } = null!;

	public DbSet<WithdrawForm> WithdrawForms { get; set; } = null!;

	public DbSet<PlayerBonusAggregate> PlayerBonusAggregates { get; set; } = null!;

	public TgsRepository(DbContextOptions<TgsRepository> options) : base(options)
	{
	}

	public IDbConnection GetDbConnection() =>
		Database.GetDbConnection();

	public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level) =>
		Database.BeginTransactionAsync(level);

	public Task<bool> CanConnectionAsync() => Database.CanConnectAsync();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.HasDefaultSchema("dbo");
		_ = modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

		_ = modelBuilder.HasPostgresExtension("pg_trgm");
	}
}
