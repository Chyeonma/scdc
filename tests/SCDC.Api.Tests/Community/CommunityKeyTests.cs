using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SCDC.BuildingBlocks.Application.Text;
using SCDC.Modules.Community.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Idempotency;
using SCDC.Modules.Community.Infrastructure.Paging;

namespace SCDC.Api.Tests.Community;

public sealed class CommunityKeyTests
{
    [Fact]
    public void Server_fingerprint_matches_the_independent_published_binary_fixture()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(CommunityMigrationTests.RepoRoot, "docs/fixtures/community-operations.json")));
        var root = fixture.RootElement;
        var example = root.GetProperty("cases")[0];
        var body = example.GetProperty("normalizedBody");
        var hash = OperationFingerprint.Compute(Convert.FromHexString(root.GetProperty("syntheticKeyHex").GetString()!),
            root.GetProperty("actorId").GetGuid(), root.GetProperty("clientOperationId").GetGuid(),
            body.GetProperty("name").GetString()!, body.GetProperty("description").GetString(), 1);
        Assert.Equal(example.GetProperty("expectedFingerprintHex").GetString(), Convert.ToHexStringLower(hash));
    }
    [Theory]
    [InlineData(0xd800)]
    [InlineData(0xdfff)]
    [InlineData(0)]
    public void Invalid_utf16_and_nul_cannot_enter_text_policy(int scalar) => Assert.False(UnicodeTextPolicy.IsValidUnicode((char)scalar + "name"));

    [Fact]
    public void Cursor_checks_expiry_purpose_and_inaccessible_storage_and_can_be_read_by_another_provider()
    {
        var path = Path.Combine(Path.GetTempPath(), $"scdc-cursor-test-{Guid.NewGuid():N}");
        var clock = new MutableClock(DateTimeOffset.UtcNow);
        ServiceProvider Build(string purpose)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().SetApplicationName(purpose).PersistKeysToFileSystem(new DirectoryInfo(path));
            return services.BuildServiceProvider();
        }
        ServerCursorCodec Codec(ServiceProvider services) => new(services.GetRequiredService<IDataProtectionProvider>(), clock,
            Options.Create(new CommunityOptions { KeyRingPath = path }), services.GetRequiredService<IOptions<KeyManagementOptions>>());
        try
        {
            using var first = Build("CursorFixture");
            var codec = Codec(first);
            var actor = Guid.CreateVersion7();
            var last = Guid.CreateVersion7();
            var token = codec.Encode(actor, 20, last);
            using var restarted = Build("CursorFixture");
            Assert.Equal(last, Codec(restarted).Decode(token, actor, 20));
            using var otherPurpose = Build("OtherPurpose");
            Assert.Throws<InvalidCursorException>(() => Codec(otherPurpose).Decode(token, actor, 20));
            clock.Advance(TimeSpan.FromHours(24));
            Assert.Throws<InvalidCursorException>(() => codec.Decode(token, actor, 20));
            Directory.Delete(path, true);
            File.WriteAllText(path, "not a directory");
            Assert.Throws<CursorKeyUnavailableException>(() => codec.Encode(actor, 20, last));
            Assert.Throws<CursorKeyUnavailableException>(() => codec.Decode(token, actor, 20));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); if (File.Exists(path)) File.Delete(path); }
    }
    internal sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now = initial;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
