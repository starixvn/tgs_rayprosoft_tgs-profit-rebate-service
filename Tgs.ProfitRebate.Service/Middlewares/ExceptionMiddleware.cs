using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using TGS.Core.Infrastructures.Extensions;
using Tgs.Logging;
using Tgs.MessageQueue;
using Tgs.ProfitRebate.Core.Models;

namespace Tgs.ProfitRebate.Service.Middlewares
{
    public class ExceptionMiddleware
    {
        private static readonly string[] _MethodNotifies =
        {
            "CreateAgentDividendsAsync",
            "CreatePlayerAggregateAsync",
            "CreatePlayerRebatesAsync",
            "CreateRoundAggregateAsync"
        };

        private readonly IMessageSender _messageSender;

        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next, IMessageSender messageSender)
        {
            _next = next;
            _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        }

        public async Task InvokeAsync(HttpContext context, ITgsLog log)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var endpoint = context.GetEndpoint();
                var name = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.MethodInfo.Name;

                var message = exception.GetMessageChain();
                log.SetLogger(name);
                log.LogError($"Report api error on :{context.Request.Host} with message: {message}", exception);

                if (_MethodNotifies.Contains(name))
                {
                    await _messageSender.PublishAsync("report.error",
                            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new ErrorMessage(message))))
                        .ConfigureAwait(false);
                }

                await context.Response.WriteAsJsonAsync(new MessageResponse
                {
                    Message = message,
                    Valid = false
                });
            }
        }
    }
}