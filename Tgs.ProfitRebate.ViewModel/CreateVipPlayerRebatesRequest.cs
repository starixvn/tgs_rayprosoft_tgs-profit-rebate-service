using System;
using MediatR;

namespace Tgs.ProfitRebate.ViewModel
{
    public record CreateVipPlayerRebatesRequest(string? Date,Guid BrandId) : IRequest<MessageResponse>;
}