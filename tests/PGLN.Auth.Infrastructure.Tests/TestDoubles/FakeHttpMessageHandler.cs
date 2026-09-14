using System.Net;

namespace PGLN.Auth.Infrastructure.Tests.TestDoubles;

internal sealed class FakeHttpMessageHandler
    : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage>
        _responses = new();

    public List<CapturedHttpRequest> Requests { get; } =
        new();

    public void Enqueue(
        HttpResponseMessage response)
    {
        _responses.Enqueue(
            response);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var body =
            request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(
                    cancellationToken);

        Requests.Add(
            new CapturedHttpRequest(
                request.Method,
                request.RequestUri?.ToString(),
                body,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter));

        if (_responses.Count == 0)
        {
            return new HttpResponseMessage(
                HttpStatusCode.InternalServerError);
        }

        return _responses.Dequeue();
    }
}

internal sealed record CapturedHttpRequest(
    HttpMethod Method,
    string? RequestUri,
    string? Body,
    string? AuthorizationScheme,
    string? AuthorizationParameter);
