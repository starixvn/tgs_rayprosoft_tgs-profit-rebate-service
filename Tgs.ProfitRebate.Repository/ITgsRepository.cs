using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using Tgs.ProfitRebate.Repository.Models;

namespace Tgs.ProfitRebate.Repository
{
    public interface ITgsRepository
    {
        DbSet<T> Set<T>() where T : class;
        DbSet<AgentDividend> AgentDividends { get; set; }
        DbSet<AgentTransaction> AgentTransactions { get; set; }
        DbSet<AgentWallet> AgentWallets { get; set; }

        DbSet<Agent> Agents { get; set; }

        DbSet<Brand> Brands { get; set; }

        DbSet<Licensee> Licensees { get; set; }

        DbSet<PlayerAggregate> PlayerAggregates { get; set; }

        DbSet<Player> Players { get; set; }

        DbSet<Reseller> Resellers { get; set; }

        DbSet<RoundAggregate> RoundAggregates { get; set; }

        DbSet<TransactionPromotion> TransactionPromotions { get; set; }

        DbSet<Transaction> Transactions { get; set; }

        DbSet<User> Users { get; set; }

        DbSet<WithdrawForm> WithdrawForms { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        IDbConnection GetDbConnection();
        IDbContextTransaction CurrentTransaction { get; }
    }
}
