using Tgs.ProfitRebate.Repository.Enums;

namespace Tgs.ProfitRebate.Repository
{
    public interface IAgentWalletRepository
    {
        ValueTask<(Guid TransactionId, decimal PostBalance)> ExchangeAsync(
            Guid brandId,
            Guid walletId,
            Guid agentId,
            TransactionType transactionType,
            decimal amount,
            string? description = null);

        ValueTask<decimal> GetBalanceAsync(Guid walletId, Guid agentId);
    }
}