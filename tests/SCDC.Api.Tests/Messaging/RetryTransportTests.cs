using System.Net.Http.Json;
using System.Text.Json;

namespace SCDC.Api.Tests.Messaging;

[Collection("DM database")]
public sealed class RetryTransportTests(TextMessageFixture f) : IClassFixture<TextMessageFixture>
{
    private HttpRequestMessage Request(Guid space, Guid operation, string content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/direct-conversations/{space}/messages")
            { Content = JsonContent.Create(new { clientMessageId = operation, content }) };
        request.Headers.Authorization = new("Bearer", f.D.Inner.Actor.AccessToken); return request;
    }

    [Fact]
    public async Task Transport_loses_committed_response_then_two_explicit_retries_return_one_ID()
    {
        var space = await f.Open("bao"); var operation = Guid.NewGuid();
        var lost = new DropAfterSuccess(space);
        using var client = f.D.Inner.Factory.CreateDefaultClient(lost);
        using var request = Request(space, operation, "LOST-RESPONSE-C02");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.NotEqual(Guid.Empty, lost.MessageId); Assert.Equal("1:1:1:1", await f.Counts(space));
        var replies = await Task.WhenAll(f.Send(space, f.D.Inner.Actor, operation, "LOST-RESPONSE-C02"),
            f.Send(space, f.D.Inner.Actor, operation, "LOST-RESPONSE-C02"));
        Assert.All(replies, reply => Assert.Equal(lost.MessageId, reply.GetProperty("id").GetGuid()));
        Assert.Equal("1:1:1:1", await f.Counts(space));
        var conflict = await f.Send(space, f.D.Inner.Actor, operation, "ALTERED-C03", 409);
        Assert.Equal("OPERATION_CONFLICT", conflict.GetProperty("errorCode").GetString());
        await f.Send(space, f.D.Inner.Actor, Guid.NewGuid(), "LOST-RESPONSE-C02"); Assert.Equal("2:2:2:2", await f.Counts(space));
    }

    [Fact]
    public async Task Rollback_503_then_same_key_retry_commits_once_and_history_sees_one_row()
    {
        var space = await f.Open("search01"); var operation = Guid.NewGuid();
        await f.Fault(space, true);
        try {
            var failed = await f.Send(space, f.D.Inner.Actor, operation, "RETRY-ROLLBACK-C01", 503);
            Assert.Equal("AUTHORITY_UNAVAILABLE", failed.GetProperty("errorCode").GetString()); Assert.Equal("0:0:0:0", await f.Counts(space));
        } finally { await f.Fault(space, false); }
        var first = await f.Send(space, f.D.Inner.Actor, operation, "RETRY-ROLLBACK-C01");
        var replay = await f.Send(space, f.D.Inner.Actor, operation, "RETRY-ROLLBACK-C01");
        Assert.Equal(first.GetProperty("id").GetGuid(), replay.GetProperty("id").GetGuid()); Assert.Equal("1:1:1:1", await f.Counts(space));
        using var history = await f.D.Inner.Send($"/api/v1/direct-conversations/{space}/messages", f.D.Inner.Users["search01"]);
        Assert.Equal(200, (int)history.StatusCode);
        Assert.Single((await history.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
    }

    private sealed class DropAfterSuccess(Guid space) : DelegatingHandler
    {
        public Guid MessageId { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == $"/api/v1/direct-conversations/{space}/messages")
            {
                Assert.Equal(200, (int)response.StatusCode);
                MessageId = (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("id").GetGuid();
                response.Dispose(); throw new HttpRequestException("Scoped test transport lost the committed response.");
            }
            return response;
        }
    }
}
