using MediatR;

namespace Tgs.ProfitRebate.Core.Models
{
    public class CreateAgentDividendsRequest : IRequest<MessageResponse>
    {
        public string? BrandCode { get; set; }
    }
}