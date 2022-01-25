using System.Threading.Tasks;
using Tgs.ProfitRebate.Client.Models;

namespace Tgs.ProfitRebate.Client
{
    public interface IProfitRebateClient
    {
        ValueTask<MessageResponse> CreatePlayerRebateAsync(CreatePlayerRebateRequest request);
        ValueTask<MessageResponse> CreateAgentDividendAsync(CreateAgentDividendsRequest request);
        ValueTask<MessageResponse> CreatePlayerVipRebateAsync(CreateVipPlayerRebatesRequest request);
        ValueTask<MessageResponse> CreatePlayerAllAgentRebateAsync(CreateAllAgentPlayerRebatesRequest request);
    }
}