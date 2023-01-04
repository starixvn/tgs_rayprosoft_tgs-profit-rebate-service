using MediatR;

namespace Tgs.ProfitRebate.Core.ViewModel;

public record CreatePlayerAllAgentRebatesRequest(string? Date) : IRequest<MessageResponse>;
