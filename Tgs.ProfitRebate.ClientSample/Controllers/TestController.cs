using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.ProfitRebate.Client;
using Tgs.ProfitRebate.Model;

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
        public async Task CreateAgentDividendsAsync()
        {
            var dd = await _client.CreateAgentDividendAsync(new CreateAgentDividendsRequest { BrandCode = null });
        }
    }
}