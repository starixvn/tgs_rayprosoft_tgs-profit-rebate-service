using System.Net.Http;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Json;
using Tgs.ProfitRebate.ApiClientLogger;
using Tgs.ProfitRebate.Client.Models;

namespace Tgs.ProfitRebate.Client
{
    public interface IProfitRebateClient
    {
        Task<MessageResponse> CreateAgentDividendAsync(CreateAgentDividendsRequest request);
    }

    public class ProfitRebateClient : BaseApiClient, IProfitRebateClient
    {
        public ProfitRebateClient(HttpClient client, IApiClientLogger logger,
            IJsonSerializationProvider json)
            : base(client, logger, json)
        {
        }

        public async Task<MessageResponse> CreateAgentDividendAsync(CreateAgentDividendsRequest request)
        {
            var prefix = "AgentDividend.CreateAgentDividend";
            return await ExecuteJsonRequestAsync<CreateAgentDividendsRequest, MessageResponse>(
                HttpMethod.Post,
                "/api/agentDividend/send", request, logPrefix: prefix,
                options: new() { PropertyNameCaseInsensitive = true }).ConfigureAwait(false);
        }
    }
}