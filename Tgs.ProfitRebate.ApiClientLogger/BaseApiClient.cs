using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TGS.Core.Infrastructures.Json;

namespace Tgs.ProfitRebate.ApiClientLogger
{
    public abstract class BaseApiClient
    {
        private const char Slash = '/';

        private static readonly char[] UrlTrimChars =
        {
            Slash,
            ' '
        };

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        private readonly HttpClient _client;

        protected readonly IJsonSerializationProvider Json;
        protected readonly IApiClientLogger Logger;

        protected BaseApiClient(HttpClient client, IApiClientLogger logger, IJsonSerializationProvider json)
        {
            Logger = logger;
            Json = json;
            _client = client;
        }

        protected ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            string logPrefix, TimeSpan? timeout = null) =>
            Logger == null ? SendWithTimeoutAsync(request, timeout) : SendAndLogAsync(request, logPrefix, timeout);

        protected async ValueTask<TResponse> ExecuteJsonRequestAsync<TRequest, TResponse>(HttpMethod method,
            string path,
            TRequest request, IReadOnlyDictionary<string, string> headers = null,
            IReadOnlyDictionary<string, string> contentHeaders = null,
            string logPrefix = null, TimeSpan? timeout = null, JsonSerializerOptions options = null)
        {
            var requestMessage = CreateHttpRequestMessage(path, method);
            var httpRequestMessage = CreateJsonRequest(requestMessage, request, headers, contentHeaders, options);
            var response = await SendAsync(httpRequestMessage, logPrefix, timeout);
            return await ReadJsonContentAsync<TResponse>(response.Content, options);
        }

        protected async ValueTask<TResponse> ExecuteJsonRequestAsync<TRequest, TResponse>(HttpMethod method, Uri apiUrl,
            string path, TRequest request, IReadOnlyDictionary<string, string> headers = null,
            IReadOnlyDictionary<string, string> contentHeaders = null,
            string logPrefix = null, TimeSpan? timeout = null, JsonSerializerOptions options = null)
        {
            var uriString = GetUriString(apiUrl, path);
            var requestMessage = CreateHttpRequestMessage(uriString, method);
            var httpRequestMessage = CreateJsonRequest(requestMessage, request, headers, contentHeaders, options);
            var response = await SendAsync(httpRequestMessage, logPrefix, timeout);
            return await ReadJsonContentAsync<TResponse>(response.Content, options);
        }

        protected async ValueTask<TResponse> ExecuteFormRequestAsync<TResponse>(Uri apiUrl, string path,
            IReadOnlyDictionary<string, string> parameters,
            IReadOnlyDictionary<string, string> headers = null,
            IReadOnlyDictionary<string, string> contentHeaders = null,
            string logPrefix = null, TimeSpan? timeout = null,
            JsonSerializerOptions options = null)
        {
            var uriString = GetUriString(apiUrl, path);
            var requestMessage = CreateHttpRequestMessage(uriString, HttpMethod.Post);
            var httpRequestMessage = CreateFormRequest(requestMessage, headers, contentHeaders, parameters);
            var response = await SendAsync(httpRequestMessage, logPrefix, timeout);
            return await ReadJsonContentAsync<TResponse>(response.Content, options);
        }

        protected async ValueTask<TResponse> ExecuteFormRequestAsync<TResponse>(string path,
            IReadOnlyDictionary<string, string> parameters,
            IReadOnlyDictionary<string, string> headers = null,
            IReadOnlyDictionary<string, string> contentHeaders = null,
            string logPrefix = null, TimeSpan? timeout = null,
            JsonSerializerOptions options = null)
        {
            var requestMessage = CreateHttpRequestMessage(path, HttpMethod.Post);
            var httpRequestMessage = CreateFormRequest(requestMessage, headers, contentHeaders, parameters);
            var response = await SendAsync(httpRequestMessage, logPrefix, timeout);
            return await ReadJsonContentAsync<TResponse>(response.Content, options);
        }

        protected async ValueTask<Stream> GetFileAsync(string path)
            => await _client.GetStreamAsync(path).ConfigureAwait(false);

        #region Private Methods

        private async ValueTask<HttpResponseMessage> SendAndLogAsync(HttpRequestMessage request, string logPrefix,
            TimeSpan? timeout)
        {
            logPrefix ??= GetType().Name;
            // Have to serialize request content first.
            // https://github.com/dotnet/corefx/pull/19082
            var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await SendWithTimeoutAsync(request, timeout);
                sw.Stop();
                await Logger.LogHttpResponseAsync(response, (int)sw.ElapsedMilliseconds, requestContent, logPrefix);

                return response;
            }
            catch (TaskCanceledException ex)
            {
                sw.Stop();

                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(
                        $"Connect to remote host timeout {Environment.NewLine}{Environment.NewLine}{ex}"),
                    RequestMessage = request
                };

                await Logger.LogHttpResponseAsync(response, (int)sw.ElapsedMilliseconds, requestContent, logPrefix);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content =
                        new StringContent($"Internal TGS exception{Environment.NewLine}{Environment.NewLine}{ex}"),
                    RequestMessage = request
                };

                await Logger.LogHttpResponseAsync(response, (int)sw.ElapsedMilliseconds, requestContent, logPrefix);

                throw;
            }
        }

        private async ValueTask<HttpResponseMessage> SendWithTimeoutAsync(HttpRequestMessage request, TimeSpan? timeout)
        {
            timeout ??= DefaultTimeout;
            using var cts = new CancellationTokenSource(timeout.Value);
            return await _client.SendAsync(request, cts.Token);
        }

        private void PrepareJsonRequest(HttpRequestMessage request, object content, JsonSerializerOptions options)
        {
            request.Headers.Remove("Accept");
            request.Headers.Add("Accept", "application/json");

            var method = request.Method;
            if (HttpMethod.Post == method || HttpMethod.Put == method || HttpMethod.Delete == method)
            {
                if (content == null)
                {
                    return;
                }

                request.Content = new StringContent(Json.SerializeToString(content, options));
                request.Content.Headers.Remove("Content-Type");
                request.Content.Headers.Add("Content-Type", "application/json");
            }
        }

        private static void PrepareFormRequest(HttpRequestMessage request,
            IReadOnlyDictionary<string, string> parameters)
        {
            var method = request.Method;
            if (HttpMethod.Post == method || HttpMethod.Put == method || HttpMethod.Delete == method)
            {
                if (parameters?.Count > 0)
                {
                    request.Content = new FormUrlEncodedContent(parameters);
                }
            }
            else if (parameters != null)
            {
                throw new InvalidOperationException($"Unexpected to have content for method {method}");
            }
        }

        private static void AddHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string> headers,
            IReadOnlyDictionary<string, string> contentHeaders)
        {
            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    if (request.Headers.Contains(key))
                    {
                        request.Headers.Remove(key);
                    }

                    request.Headers.Add(key, value);
                }
            }

            if (contentHeaders == null || request.Content == null)
            {
                return;
            }

            foreach (var (key, value) in contentHeaders)
            {
                if (request.Content.Headers.Contains(key))
                {
                    request.Content.Headers.Remove(key);
                }

                request.Content.Headers.Add(key, value);
            }
        }

        private static HttpRequestMessage CreateHttpRequestMessage(string path, HttpMethod method) =>
            new HttpRequestMessage(method, path);

        private async Task<T> ReadJsonContentAsync<T>(HttpContent content, JsonSerializerOptions options)
        {
            var json = await content.ReadAsStringAsync();
            return Json.DeserializeFromString<T>(json, options);
        }

        private HttpRequestMessage CreateJsonRequest(HttpRequestMessage httpRequestMessage, object request,
            IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> contentHeaders,
            JsonSerializerOptions options)
        {
            PrepareJsonRequest(httpRequestMessage, request, options);
            AddHeaders(httpRequestMessage, headers, contentHeaders);
            return httpRequestMessage;
        }

        private static HttpRequestMessage CreateFormRequest(HttpRequestMessage httpRequestMessage,
            IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> contentHeaders,
            IReadOnlyDictionary<string, string> parameters)
        {
            PrepareFormRequest(httpRequestMessage, parameters);
            AddHeaders(httpRequestMessage, headers, contentHeaders);
            return httpRequestMessage;
        }

        private static string GetUriString(Uri apiUrl, string path)
        {
            var uriString = apiUrl.AbsoluteUri.Trim(UrlTrimChars);

            if (!string.IsNullOrWhiteSpace(path))
            {
                uriString += $"{Slash}{path.TrimStart(UrlTrimChars)}";
            }

            return uriString;
        }

        #endregion Private Methods
    }
}