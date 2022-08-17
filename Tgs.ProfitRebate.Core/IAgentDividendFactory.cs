using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Core
{
    public interface IAgentDividendFactory
    {
        public IAsyncEnumerable<Brand[]> GetSettlementIntervalBrands(string brandCode, DateTimeOffset queryDate);

        public (DateTimeOffset, DateTimeOffset) GetInterval(SettlementIntervalType type, DateTimeOffset startDate,
            int? dayOfMonth);

        public ValueTask SaveAgentDividendData(
            Brand[] brands, List<CreateAgentDividendModel> createModels,
            List<CreateAgentDividendModel> createAutoSendModels, List<AgentDividend> updateAgentDividends);

        public Task<(List<CreateAgentDividendModel>, List<CreateAgentDividendModel>, List<AgentDividend>, int)>
            GetAgentDividends(Brand brand, int idNumber,CancellationToken cancellationToken);
    }
}