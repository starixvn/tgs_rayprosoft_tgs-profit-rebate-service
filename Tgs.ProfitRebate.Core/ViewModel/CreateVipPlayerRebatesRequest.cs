using MediatR;

namespace Tgs.ProfitRebate.Core.ViewModel;

public record CreateVipPlayerRebatesRequest(string? Date) : IRequest<MessageResponse>;