using MediatR;

namespace Tgs.ProfitRebate.ViewModel
{
    public record CreateVipPlayerRebatesRequest(string BrandCode, string? Date) : IRequest<MessageResponse>;
}