using System.ComponentModel;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.ProfitRebate.Core.ViewModel;

namespace Tgs.ProfitRebate.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AgentDividendController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentDividendController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send")]
    [Description("send agent dividends")]
    public Task<MessageResponse> CreateAgentDividendsAsync(
        CreateAgentDividendsRequest request)
        => _mediator.Send(request);
}