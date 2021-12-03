using MediatR;

namespace Tgs.ProfitRebate.Model
{
    public class CreateAgentDividendsRequest : IRequest<MessageResponse>
    {
        public string? BrandCode { get; set; }
    }
}