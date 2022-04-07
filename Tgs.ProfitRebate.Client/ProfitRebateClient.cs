using System.Text;
using System.Text.Json;
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

    public class ProfitRebateClient : IProfitRebateClient
    {
        private readonly HttpClient _client;

        public ProfitRebateClient(HttpClient client)
        {
            _client = client;
        }

        public async ValueTask<MessageResponse> CreatePlayerRebateAsync(CreatePlayerRebateRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/playerRebate/vip")
            {
                Content =
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }


        public async ValueTask<MessageResponse> CreateAgentDividendAsync(CreateAgentDividendsRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agentDividend/send")
            {
                Content =
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async ValueTask<MessageResponse> CreatePlayerAllAgentRebateAsync(
            CreateAllAgentPlayerRebatesRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/playerRebate/allAgent")
            {
                Content =
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async ValueTask<MessageResponse> CreatePlayerVipRebateAsync(CreateVipPlayerRebatesRequest request)
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/playerRebate/vip")
            {
                Content =
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
            };
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }
}