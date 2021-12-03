using Tgs.Logging;
using TGS.Core.Infrastructures.Extensions;

namespace Tgs.ProfitRebate.ApiClientLogger
{
    public interface IApiClientLogger
    {
        void LogHttpResponse(ApiLogEvent apiLogEvent);

        Task LogHttpResponseAsync(HttpResponseMessage response, int executionTimeMs, string requestContent, string logPrefix);
    }

    public sealed class ApiClientLogger : IApiClientLogger
    {
        private readonly ITgsLog _log;

        public ApiClientLogger(ITgsLog log)
        {
            _log = log;
        }

        void IApiClientLogger.LogHttpResponse(ApiLogEvent apiLogEvent)
        {
            _log.LogApi(apiLogEvent);
        }

        async Task IApiClientLogger.LogHttpResponseAsync(HttpResponseMessage response, int executionTimeMs, string requestContent, string logPrefix)
        {
            var request = response.RequestMessage;
            var resMessage = await response.Content?.ReadAsStringAsync();

            var apiCallLogEvent = new ApiLogEvent
            {
                Ip = string.Empty,
                Path = request.RequestUri.AbsoluteUri.NoLongerThan(512),
                Method = request.Method.ToString().NoLongerThan(8),
                Prefix = logPrefix.IsNullOrEmpty() ? GetPrefix(request.RequestUri.AbsolutePath) : logPrefix,
                ServerName = Environment.MachineName.NoLongerThan(128),
                RequestHeaders = request.Headers + request.Content?.Headers?.ToString(),
                RequestContent = requestContent,
                ResponseHeaders = response.Headers.ToString(),
                ResponseContent = resMessage,
                ResponseCode = (short)response.StatusCode,
                ExecutionTimeMs = executionTimeMs,
                RecordedOn = DateTimeOffset.UtcNow
            };

            _log.LogApi(apiCallLogEvent);
        }

        private static string GetPrefix(string path)
        {
            var arr = path.Split('/');
            return arr.Length < 3 ? arr[^1] : arr[2];
        }
    }
}
