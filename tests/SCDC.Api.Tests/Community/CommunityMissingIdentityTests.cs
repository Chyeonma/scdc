using System.Net;
using System.Text.Json;

namespace SCDC.Api.Tests.Community;

public sealed partial class CommunityApiTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SchemaBoundary_community_keeps_membership_owner_and_operations_when_Identity_user_is_missing(bool ownerMissing)
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        (await JoinAsync(server)).EnsureSuccessStatusCode();
        var before = await SqlAsync("""
            SELECT jsonb_build_object('server',to_jsonb(s),
                'members',(SELECT jsonb_agg(to_jsonb(m) ORDER BY user_id) FROM community.server_members m WHERE server_id=s.id),
                'operations',(SELECT jsonb_agg(to_jsonb(o) ORDER BY client_operation_id) FROM community.operations o WHERE resource_id=s.id))::text
            FROM community.servers s WHERE id=@id
            """, ("id", server));
        var missing = ownerMissing ? _owner : _other;
        var viewer = ownerMissing ? _other : _owner;
        await SqlAsync("DELETE FROM identity.users WHERE id=@id", ("id", missing.UserId));
        var after = await SqlAsync("""
            SELECT jsonb_build_object('server',to_jsonb(s),
                'members',(SELECT jsonb_agg(to_jsonb(m) ORDER BY user_id) FROM community.server_members m WHERE server_id=s.id),
                'operations',(SELECT jsonb_agg(to_jsonb(o) ORDER BY client_operation_id) FROM community.operations o WHERE resource_id=s.id))::text
            FROM community.servers s WHERE id=@id
            """, ("id", server));
        Assert.Equal(before, after);
        using var detail = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}", viewer);
        detail.EnsureSuccessStatusCode();
        Assert.Equal(_owner.UserId, (await JsonAsync(detail)).GetProperty("ownerUserId").GetGuid());
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}/members", viewer);
        if (ownerMissing)
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); // No automatic ownership/permission transfer.
        else
        {
            response.EnsureSuccessStatusCode();
            var orphan = (await JsonAsync(response)).GetProperty("items").EnumerateArray()
                .Single(item => item.GetProperty("membership").GetProperty("userId").GetGuid() == missing.UserId);
            Assert.Equal(JsonValueKind.Null, orphan.GetProperty("user").ValueKind);
            Assert.NotEqual(Guid.Empty, orphan.GetProperty("membership").GetProperty("membershipId").GetGuid());
        }
        using var denied = await SendAsync(HttpMethod.Get, $"/api/v1/servers/{server}", missing);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    [Fact]
    public async Task SchemaBoundary_community_channel_does_not_cascade_when_Messaging_space_is_missing()
    {
        var server = (await JsonAsync(await CreateAsync())).GetProperty("id").GetGuid();
        var channel = (await JsonAsync(await ChannelCreateAsync(server))).GetProperty("id").GetGuid();
        var before = await SqlAsync("SELECT to_jsonb(c)::text FROM community.channels c WHERE space_id=@id", ("id", channel));
        await SqlAsync("DELETE FROM messaging.spaces WHERE id=@id", ("id", channel));
        Assert.Equal(before, await SqlAsync("SELECT to_jsonb(c)::text FROM community.channels c WHERE space_id=@id", ("id", channel)));
        using var response = await ChannelGetAsync(server, channel);
        response.EnsureSuccessStatusCode();
        Assert.Equal(channel, (await JsonAsync(response)).GetProperty("id").GetGuid());
    }
}
