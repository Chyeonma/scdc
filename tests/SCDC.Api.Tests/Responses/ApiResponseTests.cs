using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Responses;

public sealed class ApiResponseTests(SCDCWebApplicationFactory factory)
    : IClassFixture<SCDCWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_typed_success_response()
    {
        var response = await _client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Business_error_returns_standard_problem_details()
    {
        var response = await _client.GetAsync("/_tests/responses/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Assert.Equal("Identity.UserNotFound", root.GetProperty("errorCode").GetString());
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Invalid_request_returns_validation_problem_details()
    {
        var response = await _client.GetAsync("/_tests/responses/validation");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Common.ValidationFailed", problem.GetProperty("errorCode").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("value", out _));
    }

    [Fact]
    public async Task Unhandled_exception_returns_safe_problem_details()
    {
        var response = await _client.GetAsync("/_tests/responses/exception");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        Assert.Equal(
            "Common.UnexpectedError",
            document.RootElement.GetProperty("errorCode").GetString());
        Assert.DoesNotContain("Sensitive exception detail", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_route_returns_standard_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Common.NotFound", problem.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Swagger_documents_the_standard_error_contract()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/health")
            .GetProperty("get")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("500", out var serverError));
        Assert.Contains("ApiProblemDetails", serverError.GetRawText(), StringComparison.Ordinal);
        Assert.True(
            serverError.GetProperty("content").TryGetProperty("application/problem+json", out _));
    }
}
