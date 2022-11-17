using System.ComponentModel;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tgs.ProfitRebate.ViewModel;

namespace Tgs.ProfitRebate.Service.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlayerRebateController : ControllerBase
{
	private readonly IMediator _mediator;

	public PlayerRebateController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpPost("vip")]
	[Description("create vip player rebate")]
	public Task<MessageResponse> CreateVipPlayerRebatesAsync(
		CreateVipPlayerRebatesRequest request)
		=> _mediator.Send(request);

	[HttpPost("allAgent")]
	[Description("create all agent player rebate")]
	public Task<MessageResponse> CreateAllAgentPlayerRebatesAsync(
		CreatePlayerAllAgentRebatesRequest request)
		=> _mediator.Send(request);
}
