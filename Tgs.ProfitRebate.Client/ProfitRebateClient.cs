using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tgs.ProfitRebate.Client.Models;

namespace Tgs.ProfitRebate.Client
{
    public interface IProfitRebateClient
    {
        Task<MessageResponse?> CreateAgentDividendAsync(CreateAgentDividendsRequest request);
        Task<MessageResponse?> CreatePlayerVipRebateAsync(CreateVipPlayerRebatesRequest request);
        Task<MessageResponse?> CreatePlayerAllAgentRebateAsync(CreateAllAgentPlayerRebatesRequest request);
    }

    public class ProfitRebateClient : IProfitRebateClient
    {
        private readonly HttpClient _client;

        public ProfitRebateClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<MessageResponse?> CreateAgentDividendAsync(CreateAgentDividendsRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agentDividend/send");
            httpRequest.Content =
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<MessageResponse?> CreatePlayerAllAgentRebateAsync(CreateAllAgentPlayerRebatesRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/playerRebate/allAgent");
            httpRequest.Content =
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync());
        }

        public async Task<MessageResponse?> CreatePlayerVipRebateAsync(CreateVipPlayerRebatesRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/playerRebate/vip");
            httpRequest.Content =
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync());
        }
    }
}