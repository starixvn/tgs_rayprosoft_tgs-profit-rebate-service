using MediatR;

namespace Tgs.ProfitRebate.ViewModel
{
    public record CreateVipPlayerRebatesRequest(string? Date) : IRequest<MessageResponse>;
}