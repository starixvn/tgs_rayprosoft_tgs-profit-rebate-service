using MediatR;

namespace Tgs.ProfitRebate.ViewModel
{
	public record CreatePlayerAllAgentRebatesRequest(string? Date) : IRequest<MessageResponse>;
}
