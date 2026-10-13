using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using SCDC.Api.Tests.Infrastructure;
using SCDC.Contracts.Identity;

namespace SCDC.Api.Tests.Messaging;

public sealed class ModuleIntegrationTests
{
    [Fact]
    public async Task Community_and_dm_resolve_their_own_account_guards()
    {
        using var factory = new SCDCWebApplicationFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();

        Assert.Equal("AccountAccessGuard", scope.ServiceProvider.GetRequiredService<IAccountAccessGuard>().GetType().Name);
        Assert.Equal("AccountPairAccessGuard", scope.ServiceProvider.GetRequiredService<IAccountPairAccessGuard>().GetType().Name);
    }

    [Fact]
    public void Dm_cursors_use_an_isolated_provider_when_community_is_registered()
    {
        using var factory = new SCDCWebApplicationFactory();
        using var client = factory.CreateClient();
        var community = factory.Services.GetRequiredService<IDataProtectionProvider>();
        var dm = factory.Services.GetRequiredKeyedService<IDataProtectionProvider>("Messaging.Cursors");
        var protectedCursor = dm.CreateProtector("merge-provider-isolation").Protect("dm-cursor");

        Assert.NotSame(community, dm);
        Assert.Equal("dm-cursor", dm.CreateProtector("merge-provider-isolation").Unprotect(protectedCursor));
        Assert.Throws<CryptographicException>(() => community.CreateProtector("merge-provider-isolation").Unprotect(protectedCursor));
    }
}
