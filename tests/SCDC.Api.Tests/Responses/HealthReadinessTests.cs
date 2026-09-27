using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SCDC.Api.Controllers;
using SCDC.BuildingBlocks.Application;

namespace SCDC.Api.Tests.Responses;

public sealed class HealthReadinessTests
{
    [Fact]
    public async Task Ready_returns_503_when_database_is_unreachable()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] =
                    "Host=127.0.0.1;Port=1;Database=scdc;Username=scdc;Password=test;Timeout=1"
            })
            .Build();
        var controller = new HealthController([], TimeProvider.System, configuration);

        var result = await controller.Ready(CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, unavailable.StatusCode);
    }
}
