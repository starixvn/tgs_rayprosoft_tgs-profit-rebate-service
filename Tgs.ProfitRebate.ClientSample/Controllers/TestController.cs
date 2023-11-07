using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Tgs.ProfitRebate.Client;
using Tgs.ProfitRebate.Client.Models;

namespace Tgs.ProfitRebate.ClientSample.Controllers;

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
    public ValueTask<MessageResponse> CreateAgentDividendsAsync()
        => _client.CreateAgentDividendAsync(new(null));

    [HttpPost("vipRebate")]
    [Description("Create player rebates")]
    public ValueTask<MessageResponse> CreatePlayerRebatesAsync()
        => _client.CreatePlayerRebateAsync(new());

    [HttpPost("allAgentRebate")]
    [Description("Create all agent rebates")]
    public ValueTask<MessageResponse> CreatePlayerAllAgentRebatesAsync()
        => _client.CreatePlayerAllAgentRebateAsync(new(null));
}