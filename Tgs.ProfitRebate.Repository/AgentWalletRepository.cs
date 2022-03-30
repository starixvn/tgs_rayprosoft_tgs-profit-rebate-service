using System.Transactions;
using TGS.Core.Infrastructures.Extensions;
using Tgs.ProfitRebate.Enum;

namespace Tgs.ProfitRebate.Repository
{
    internal class AgentWalletRepository : IAgentWalletRepository
    {
        private readonly ITgsRepository _tgsRepository;

        public AgentWalletRepository(ITgsRepository tgsRepository)
        {
            _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
        }

        public async ValueTask<(Guid TransactionId, decimal PostBalance)> ExchangeAsync(
            Guid brandId,
            Guid walletId,
            Guid agentId,
            TransactionType transactionType,
            decimal amount,
            string? description)
        {
            const string sql = "dbo.agent_wallet_update_wallet_balance";
            var updatedOn = DateTimeOffset.UtcNow;
            var transactionId = GuidExtensions.NewSequentialGuid();

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var postBalance = await _tgsRepository.SqlQuerySingleAsync<decimal>(
                sql,
                parameters: new Dictionary<string, object>
                {
                    { ":agent_id", agentId },
                    { ":wallet_id", walletId },
                    { ":amount", amount },
                    { ":updated_on", updatedOn }
                }).ConfigureAwait(false);

            await _tgsRepository.AgentTransactions
                .AddAsync(new()
                {
                    Id = transactionId,
                    AgentId = agentId,
                    WalletId = walletId,
                    BrandId = brandId,
                    Amount = amount,
                    Balance = postBalance,
                    Type = (short)transactionType,
                    Description = description,
                    RecordedOn = DateTimeOffset.UtcNow
                })
                .ConfigureAwait(false);

            await _tgsRepository.SaveChangesAsync().ConfigureAwait(false);

            scope.Complete();

            return (transactionId, postBalance);
        }

        public async ValueTask<decimal> GetBalanceAsync(Guid walletId, Guid agentId)
        {
            var agentWallet = await _tgsRepository.AgentWallets
                .FindAsync(walletId)
                .ConfigureAwait(false);

            if (agentWallet?.AgentId == agentId)
                return agentWallet.Balance;
            throw new Exception("Can't find agent wallet.");
        }
    }
}