using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.ProfitRebate.Client;
using Tgs.ProfitRebate.Client.Models;

namespace Tgs.ProfitRebate.ClientSample.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IProfitRebateClient _client;

        public TestController(IProfitRebateClient client)
        {
            _client = client;
        }

        [HttpPost("dividends")]
        [Description("Create agent dividends")]
        public Task<MessageResponse?> CreateAgentDividendsAsync()
            => _client.CreateAgentDividendAsync(new CreateAgentDividendsRequest(null));

        [HttpPost("allAgentRebate")]
        [Description("Create all agent rebates")]
        public Task<MessageResponse?> CreatePlayerAllAgentRebatesAsync()
            => _client.CreatePlayerAllAgentRebateAsync(new CreateAllAgentPlayerRebatesRequest(null));
    }
}