using System;
using System.ComponentModel;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.Logging;
using Tgs.ProfitRebate.Core.Models;

namespace Tgs.ProfitRebate.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentDividendController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AgentDividendController(IMediator mediator) => _mediator = mediator;

        [HttpPost("send")]
        [Description("send agent dividends")]
        public Task<MessageResponse> CreateAgentDividendsAsync(
            CreateAgentDividendsRequest request)
        {
            return _mediator.Send(request);
        }
    }
}