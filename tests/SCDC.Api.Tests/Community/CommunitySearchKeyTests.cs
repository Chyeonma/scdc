using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SCDC.BuildingBlocks.Application.Text;
using SCDC.Modules.Community.Infrastructure;
using SCDC.Modules.Community.Infrastructure.Paging;

namespace SCDC.Api.Tests.Community;

public sealed class CommunitySearchKeyTests
{
    [Theory]
    [InlineData("\u0085CAFÉ 👩‍💻\u3000", "café 👩‍💻")]
    [InlineData("ĐỘI BÓNG", "đội bóng")]
    [InlineData("𐐀 Club", "𐐨 club")]
    [InlineData("É", "é")]
    [InlineData("é", "é")]
    public void Search_key_has_fixed_unicode_casing_and_normalization_examples(string input, string expected)
        => Assert.Equal(expected, UnicodeTextPolicy.NormalizeNameKey(input));

    [Fact]
    public void Search_cursor_checks_query_actor_limit_position_expiry_purpose_and_storage_across_restart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"scdc-search-cursor-{Guid.NewGuid():N}");
        var clock = new CommunityKeyTests.MutableClock(DateTimeOffset.UtcNow);
        ServiceProvider Build(string app)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().SetApplicationName(app).PersistKeysToFileSystem(new DirectoryInfo(path));
            return services.BuildServiceProvider();
        }
        SearchCursorCodec Codec(ServiceProvider services) => new(services.GetRequiredService<IDataProtectionProvider>(), clock,
            Options.Create(new CommunityOptions { KeyRingPath = path }), services.GetRequiredService<IOptions<KeyManagementOptions>>());
        try
        {
            using var first = Build("SearchFixture");
            var codec = Codec(first);
            var actor = Guid.CreateVersion7();
            var position = new SearchPosition(0, "café", Guid.CreateVersion7());
            var token = codec.Encode(actor, "café", 20, position);
            using var restarted = Build("SearchFixture");
            Assert.Equal(position, Codec(restarted).Decode(token, actor, "café", 20));
            Assert.Throws<InvalidCursorException>(() => codec.Decode(token, Guid.CreateVersion7(), "café", 20));
            Assert.Throws<InvalidCursorException>(() => codec.Decode(token, actor, "cafe", 20));
            Assert.Throws<InvalidCursorException>(() => codec.Decode(token, actor, "café", 50));
            using var otherApp = Build("AnotherApplication");
            Assert.Throws<InvalidCursorException>(() => Codec(otherApp).Decode(token, actor, "café", 20));
            var list = new ServerCursorCodec(first.GetRequiredService<IDataProtectionProvider>(), clock,
                Options.Create(new CommunityOptions { KeyRingPath = path }), first.GetRequiredService<IOptions<KeyManagementOptions>>());
            Assert.Throws<InvalidCursorException>(() => codec.Decode(list.Encode(actor, 20, position.Id), actor, "café", 20));
            foreach (var invalid in new[] { position with { Rank = 2 }, position with { Id = Guid.Empty }, position with { Name = "" }, position with { Rank = 1 } })
                Assert.Throws<InvalidCursorException>(() => codec.Decode(codec.Encode(actor, "café", 20, invalid), actor, "café", 20));
            clock.Advance(TimeSpan.FromHours(24));
            Assert.Throws<InvalidCursorException>(() => codec.Decode(token, actor, "café", 20));
            Directory.Delete(path, true);
            File.WriteAllText(path, "not a directory");
            Assert.Throws<CursorKeyUnavailableException>(() => codec.Encode(actor, "café", 20, position));
            Assert.Throws<CursorKeyUnavailableException>(() => codec.Decode(token, actor, "café", 20));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); if (File.Exists(path)) File.Delete(path); }
    }
}
