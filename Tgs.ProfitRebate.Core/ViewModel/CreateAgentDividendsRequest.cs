using MediatR;

namespace Tgs.ProfitRebate.Core.ViewModel;

public class CreateAgentDividendsRequest : IRequest<MessageResponse>
{
	public string? BrandCode { get; init; }
}
