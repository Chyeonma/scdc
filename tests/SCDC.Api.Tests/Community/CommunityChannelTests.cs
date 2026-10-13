using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SCDC.Api.Tests.Infrastructure;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    private Task<HttpResponseMessage> ChannelCreateAsync(Guid server, string name = "Phòng chung", string? topic = null,
        Guid? operation = null, Actor? actor = null, HttpClient? client = null)
        => SendAsync(HttpMethod.Post, $"/api/v1/servers/{server}/channels", actor ?? _owner,
            new { clientOperationId = operation ?? Guid.NewGuid(), name, topic }, client);
    private Task<HttpResponseMessage> ChannelGetAsync(Guid server, Guid channel, Actor? actor = null)
        => SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}/channels/{channel}", actor ?? _owner);
    private Task<HttpResponseMessage> ChannelListAsync(Guid server, Actor? actor = null, int limit = 20, string? cursor = null, HttpClient? client = null)
        => SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}/channels?limit={limit}" + (cursor is null ? "" : "&cursor=" + Uri.EscapeDataString(cursor)), actor ?? _owner, client: client);
    private Task<HttpResponseMessage> AclAsync(Guid server, Guid channel, Actor? actor = null)
        => SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}/channels/{channel}/access", actor ?? _owner);
    private Task<HttpResponseMessage> AclPutAsync(Guid server, Guid channel, string view = "allow", object[]? roles = null,
        object[]? members = null, string version = "1", Actor? actor = null, HttpClient? client = null)
        => SendAsync(HttpMethod.Put, $"/api/v1/servers/{server}/channels/{channel}/access", actor ?? _owner,
            new { expectedAccessVersion = version, defaultView = view, roleOverrides = roles ?? [], memberOverrides = members ?? [] }, client);
    private static Guid[] ChannelIds(JsonElement page) => page.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("id").GetGuid()).ToArray();

    [Fact]
    public async Task Channel_create_space_metadata_operation_event_and_replay_are_atomic_and_current()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var op = Guid.NewGuid();
        var response = await ChannelCreateAsync(server, " Café 👩‍💻 ", "Một\r\nHai", op);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await JsonAsync(response); var channel = created.GetProperty("id").GetGuid();
        Assert.Equal(7, channel.Version); Assert.Equal("Café 👩‍💻", created.GetProperty("name").GetString());
        Assert.Equal("Một\nHai", created.GetProperty("topic").GetString());
        Assert.Equal("1", created.GetProperty("version").GetString()); Assert.Equal("1", created.GetProperty("accessVersion").GetString());
        Assert.Equal($"/api/v1/servers/{server}/channels/{channel}", response.Headers.Location?.ToString());
        Assert.Equal(1L, await SqlAsync("SELECT count(*) FROM messaging.spaces WHERE id=@id AND space_type=3 AND status=1", ("id", channel)));
        Assert.Equal(1L, await AccessEventsAsync(server));
        var edited = await SendAsync(HttpMethod.Patch, response.Headers.Location!.ToString(), _owner, new { expectedVersion = "1", name = "Mới", topic = (string?)null });
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode); Assert.Equal("2", (await JsonAsync(edited)).GetProperty("version").GetString());
        var replay = await ChannelCreateAsync(server, "Café 👩‍💻", "Một\nHai", op);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode); Assert.Equal("Mới", (await JsonAsync(replay)).GetProperty("name").GetString());
        Assert.Equal(2L, await AccessEventsAsync(server));
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Patch, response.Headers.Location.ToString(), _owner, new { expectedVersion = "2", name = "Mới" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AclPutAsync(server, channel)).StatusCode); Assert.Equal(2L, await AccessEventsAsync(server));
        await AssertErrorAsync(await ChannelCreateAsync(server, "Khác", operation: op), HttpStatusCode.Conflict, "OPERATION_CONFLICT");
        await SqlAsync("UPDATE community.channels SET status=3,deleted_at=clock_timestamp() WHERE space_id=@id; UPDATE messaging.spaces SET status=3,deleted_at=clock_timestamp() WHERE id=@id", ("id", channel));
        await AssertErrorAsync(await ChannelGetAsync(server, channel), HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertErrorAsync(await ChannelCreateAsync(server, "Café 👩‍💻", "Một\nHai", op), HttpStatusCode.Conflict, "OPERATION_RESOURCE_REMOVED");
        Assert.Equal(HttpStatusCode.Created, (await ChannelCreateAsync(server, "Café 👩‍💻")).StatusCode);
    }
    [Fact]
    public async Task Channel_management_does_not_grant_hidden_view_or_other_management_and_acl_self_removal_commits()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        await AssertErrorAsync(await ChannelListAsync(server, _other), HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        var member = await JsonAsync(await JoinAsync(server));
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        await AssertErrorAsync(await ChannelCreateAsync(server, actor: _other), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        await AssertErrorAsync(await AclAsync(server, channel, _other), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var manage = (await JsonAsync(await RoleCreateAsync(server, permissions: ["manage_channels"]))).GetProperty("id").GetGuid();
        var assigned = await JsonAsync(await RoleSetAsync(server, member, [manage]));
        Assert.Equal(HttpStatusCode.Created, (await ChannelCreateAsync(server, "Được tạo", actor: _other)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Patch, $"/api/v1/servers/{server}/channels/{channel}", _other, new { expectedVersion = "1", topic = "Topic" })).StatusCode);
        await AssertErrorAsync(await AclAsync(server, channel, _other), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        var access = (await JsonAsync(await RoleCreateAsync(server, "Access", ["manage_channel_access"]))).GetProperty("id").GetGuid();
        (await RoleSetAsync(server, assigned, [access])).EnsureSuccessStatusCode();
        await AssertErrorAsync(await ChannelCreateAsync(server, "Không tạo", actor: _other), HttpStatusCode.Forbidden, "PERMISSION_DENIED");
        Assert.Equal(HttpStatusCode.OK, (await AclAsync(server, channel, _other)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AclPutAsync(server, channel, "deny", actor: _other)).StatusCode);
        foreach (var response in new[] { await ChannelGetAsync(server, channel, _other), await AclAsync(server, channel, _other),
            await AclPutAsync(server, channel, actor: _other, version: "2"),
            await SendAsync(HttpMethod.Patch, $"/api/v1/servers/{server}/channels/{channel}", _other, new { expectedVersion = "3", name = "Hidden" }) })
            await AssertErrorAsync(response, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        Assert.DoesNotContain(channel, ChannelIds(await JsonAsync(await ChannelListAsync(server, _other))));
        Assert.Equal(HttpStatusCode.OK, (await ChannelGetAsync(server, channel)).StatusCode);
    }
    [Theory]
    [InlineData("allow", 0, 0, true)]
    [InlineData("deny", 0, 0, false)]
    [InlineData("deny", 1, 0, true)]
    [InlineData("allow", 2, 0, false)]
    [InlineData("allow", 3, 0, false)]
    [InlineData("deny", 3, 1, true)]
    [InlineData("allow", 1, 2, false)]
    [InlineData("deny", 0, 1, true)]
    [InlineData("allow", 4, 0, false)]
    [InlineData("deny", 4, 1, true)]
    public async Task Channel_list_and_detail_match_default_role_deny_wins_and_personal_priority(string view, int roles, int personal, bool visible)
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var member = await JsonAsync(await JoinAsync(server));
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        var allow = (await JsonAsync(await RoleCreateAsync(server, "Allow"))).GetProperty("id").GetGuid();
        var deny = (await JsonAsync(await RoleCreateAsync(server, "Deny"))).GetProperty("id").GetGuid();
        (await RoleSetAsync(server, member, [allow, deny])).EnsureSuccessStatusCode();
        var overrides = new List<object>();
        if ((roles & 1) != 0) overrides.Add(new { roleId = allow, effect = "allow" });
        if ((roles & 2) != 0) overrides.Add(new { roleId = deny, effect = "deny" });
        if ((roles & 4) != 0) overrides.Add(new { roleId = (await JsonAsync(await RoleListAsync(server))).GetProperty("items")[0].GetProperty("id").GetGuid(), effect = "deny" });
        object[] members = personal == 0 ? [] : [new { userId = _other.UserId, membershipId = member.GetProperty("membershipId").GetGuid(), effect = personal == 1 ? "allow" : "deny" }];
        (await AclPutAsync(server, channel, view, overrides.ToArray(), members)).EnsureSuccessStatusCode();
        Assert.Equal(visible, ChannelIds(await JsonAsync(await ChannelListAsync(server, _other))).Contains(channel));
        Assert.Equal(visible ? HttpStatusCode.OK : HttpStatusCode.NotFound, (await ChannelGetAsync(server, channel, _other)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ChannelGetAsync(server, channel)).StatusCode);
    }
    [Fact]
    public async Task Acl_full_replacement_rejects_cross_scope_duplicates_stale_epochs_and_versions_without_partial_state()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); var member = await JsonAsync(await JoinAsync(server));
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        var system = (await JsonAsync(await RoleListAsync(server))).GetProperty("items")[0].GetProperty("id").GetGuid();
        var foreignServer = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var foreign = (await JsonAsync(await RoleCreateAsync(foreignServer))).GetProperty("id").GetGuid();
        foreach (var overrides in new object[][] { [new { roleId = foreign, effect = "allow" }], [new { roleId = system, effect = "inherit" }],
            [new { roleId = system, effect = "allow" }, new { roleId = system, effect = "deny" }] })
            await AssertErrorAsync(await AclPutAsync(server, channel, roles: overrides), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        await AssertErrorAsync(await AclPutAsync(server, channel, members: [new { userId = _other.UserId, membershipId = Guid.CreateVersion7(), effect = "allow" }]), HttpStatusCode.Conflict, "MEMBERSHIP_CHANGED");
        await AssertErrorAsync(await AclPutAsync(server, channel, members: [new { userId = Guid.CreateVersion7(), membershipId = Guid.CreateVersion7(), effect = "allow" }]), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        Assert.Equal("1", (await JsonAsync(await AclAsync(server, channel))).GetProperty("accessVersion").GetString());
        var changed = await AclPutAsync(server, channel, "deny", [new { roleId = system, effect = "allow" }]);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode); Assert.Equal("2", (await JsonAsync(changed)).GetProperty("accessVersion").GetString());
        await AssertErrorAsync(await AclPutAsync(server, channel), HttpStatusCode.Conflict, "VERSION_CONFLICT");
        Assert.Equal(HttpStatusCode.OK, (await AclPutAsync(server, channel, "deny", [new { roleId = system, effect = "allow" }], version: "2")).StatusCode);
        Assert.Equal(2L, await AccessEventsAsync(server));
        await AssertErrorAsync(await ChannelGetAsync(foreignServer, channel), HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch, $"/api/v1/servers/{server}/channels/{channel}", _owner, new { expectedVersion = "1", name = "Stale" }), HttpStatusCode.Conflict, "VERSION_CONFLICT");
    }
    [Fact]
    public async Task Channel_unicode_collision_topic_limits_and_kind_immutability_are_enforced()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var first = await JsonAsync(await ChannelCreateAsync(server, "Café", "😀"));
        await AssertErrorAsync(await ChannelCreateAsync(server, "CAFE\u0301"), HttpStatusCode.Conflict, "NAME_CONFLICT");
        Assert.Equal(HttpStatusCode.Created, (await ChannelCreateAsync(server, "Cafe")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await ChannelCreateAsync(server, string.Concat(Enumerable.Repeat("😀", 50)), new string('x', 1000))).StatusCode);
        foreach (var name in new[] { "", "\u200b\u034f", "OK\nX", "OK\0", new string('x', 101) })
            await AssertErrorAsync(await ChannelCreateAsync(server, name), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        await AssertErrorAsync(await ChannelCreateAsync(server, topic: new string('x', 1001)), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Post, $"/api/v1/servers/{server}/channels", _owner, new { clientOperationId = Guid.NewGuid(), name = "Voice", kind = "voice" }), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch, $"/api/v1/servers/{server}/channels/{first.GetProperty("id").GetGuid()}", _owner, new { expectedVersion = "1", kind = "voice" }), HttpStatusCode.BadRequest, "Common.ValidationFailed");
        await AssertErrorAsync(await SendAsync(HttpMethod.Patch, $"/api/v1/servers/{server}/channels/{first.GetProperty("id").GetGuid()}", _owner, new { expectedVersion = "01", topic = "No" }), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    }
    [Fact]
    public async Task Channel_pages_filter_hidden_before_limit_recheck_access_and_bind_cursor_to_scope()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid(); await JoinAsync(server);
        var ids = new List<Guid>();
        for (var i = 0; i < 4; i++) ids.Add((await JsonAsync(await ChannelCreateAsync(server, $"Phòng {i}"))).GetProperty("id").GetGuid());
        (await AclPutAsync(server, ids[0], "deny")).EnsureSuccessStatusCode();
        var first = await JsonAsync(await ChannelListAsync(server, _other, 1)); Assert.Equal(ids[1], ChannelIds(first).Single());
        var cursor = first.GetProperty("nextCursor").GetString()!;
        (await AclPutAsync(server, ids[2], "deny")).EnsureSuccessStatusCode();
        var next = await JsonAsync(await ChannelListAsync(server, _other, 1, cursor)); Assert.Equal(ids[3], ChannelIds(next).Single());
        Assert.Equal(JsonValueKind.Null, next.GetProperty("nextCursor").ValueKind);
        await AssertErrorAsync(await ChannelListAsync(server, limit: 1, cursor: cursor), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await ChannelListAsync(server, _other, 2, cursor), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await AssertErrorAsync(await RoleListAsync(server, limit: 1, cursor: cursor), HttpStatusCode.BadRequest, "CURSOR_INVALID");
        await using var restart = new SCDCWebApplicationFactory(); using var client = restart.CreateClient();
        Assert.Equal(ids[3], ChannelIds(await JsonAsync(await ChannelListAsync(server, _other, 1, cursor, client))).Single());
        await AssertErrorAsync(await ChannelListAsync(server, limit: 0), HttpStatusCode.BadRequest, "VALIDATION_FAILED");
    }
    [Theory]
    [InlineData("unverified", HttpStatusCode.Forbidden)]
    [InlineData("revoked", HttpStatusCode.Unauthorized)]
    public async Task Channel_owner_still_requires_current_identity(string change, HttpStatusCode status)
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        await SqlAsync(change == "unverified" ? "UPDATE identity.user_emails SET verified_at=NULL WHERE user_id=@id" : "UPDATE identity.auth_sessions SET revoked_at=clock_timestamp() WHERE user_id=@id", ("id", _owner.UserId));
        Assert.Equal(status, (await ChannelGetAsync(server, channel)).StatusCode); Assert.Equal(status, (await ChannelListAsync(server)).StatusCode);
        Assert.Equal(status, (await ChannelCreateAsync(server, "No")).StatusCode); Assert.Equal(1L, await AccessEventsAsync(server));
    }
}
