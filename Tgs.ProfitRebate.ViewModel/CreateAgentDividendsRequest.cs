using MediatR;

namespace Tgs.ProfitRebate.ViewModel
{
    public class CreateAgentDividendsRequest : IRequest<MessageResponse>
    {
        public string? BrandCode { get; init; }
    }
}