using Tgs.ProfitRebate.Core.ViewModel;
using Tgs.ProfitRebate.Enum;
using Tgs.ProfitRebate.Repository.Entities;

namespace Tgs.ProfitRebate.Core;

public interface IAgentDividendFactory
{
    public IAsyncEnumerable<Brand[]> GetSettlementIntervalBrands(string brandCode, DateTimeOffset queryDate);

    public (DateTimeOffset, DateTimeOffset) GetInterval(SettlementIntervalType type, DateTimeOffset startDate,
        int? dayOfMonth);

    public ValueTask SaveAgentDividendData(
        Brand[] brands, List<CreateAgentDividendModel> createAutoSendModels, List<AgentDividend> updateAgentDividends);

    public Task<(List<CreateAgentDividendModel>, int)>
        GetAgentDividends(Brand brand, int idNumber, CancellationToken cancellationToken);

    public Task<(List<CreateAgentDividendModel>, List<AgentDividend>)>
        GetNoSendDividends(List<CreateAgentDividendModel> agentDividends, CancellationToken cancellationToken);
}