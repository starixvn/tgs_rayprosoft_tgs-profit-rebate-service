using Tgs.ProfitRebate.Model;
using Tgs.ProfitRebate.Repository.Enums;
using Tgs.ProfitRebate.Repository.Models;

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

        public Task<(List<CreateAgentDividendModel>, List<CreateAgentDividendModel>, List<AgentDividend>)>
            GetAgentDividends(Brand brand, int idNumber);
    }
}