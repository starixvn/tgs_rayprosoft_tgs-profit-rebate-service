using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository
{
    public class TgsRepository : DbContext, ITgsRepository
    {
        public TgsRepository(DbContextOptions<TgsRepository> options) : base(options)
        {
        }

        public DbSet<PlayerAllAgentRebate> PlayerAllAgentRebates { get; set; }

        public DbSet<AgentDividend> AgentDividends { get; set; }

        public DbSet<Agent> Agents { get; set; }

        public DbSet<Brand> Brands { get; set; }

        public IDbContextTransaction CurrentTransaction =>
            Database.CurrentTransaction;

        public DbSet<Licensee> Licensees { get; set; }

        public DbSet<PlayerAggregate> PlayerAggregates { get; set; }
        public DbSet<PlayerGameAggregate> PlayerGameAggregates { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Reseller> Resellers { get; set; }

        public DbSet<RoundAggregate> RoundAggregates { get; set; }

        public DbSet<TransactionPromotion> TransactionPromotions { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<WithdrawForm> WithdrawForms { get; set; }
        public DbSet<AgentTransaction> AgentTransactions { get; set; }
        public DbSet<AgentWallet> AgentWallets { get; set; }
        public DbSet<BrandGame> BrandGames { get; set; }
        public DbSet<BrandAllAgent> BrandAllAgents { get; set; }
        public DbSet<BrandAllAgentGameType> BrandAllAgentGameTypes { get; set; }
        public DbSet<BrandAllAgentGameTypeCondition> BrandAllAgentGameTypeConditions { get; set; }

        public DbSet<PlayerParent> PlayerParents { get; set; }

        public IDbConnection GetDbConnection() =>
            Database.GetDbConnection();

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level) =>
            Database.BeginTransactionAsync(level);

        public Task<bool> CanConnectionAsync() => Database.CanConnectAsync();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            modelBuilder.HasPostgresExtension("pg_trgm");
        }
    }
}