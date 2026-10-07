using System.Text.Json;
using System.Text.Json.Serialization;
using SCDC.Modules.Community.Features.Permissions.Domain;
using SCDC.Modules.Community.Infrastructure.Idempotency;

namespace SCDC.Api.Tests.Community;

public sealed class CommunityPermissionPolicyTests
{
    [Fact]
    public void Channel_fingerprint_matches_published_unicode_and_nullable_topic_fixture()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(Path.Combine(CommunityMigrationTests.RepoRoot, "docs/fixtures/community-operations.json")));
        var root = data.RootElement;
        var item = root.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("operationKind").GetString() == "create_channel");
        var body = item.GetProperty("normalizedBody");
        var key = Convert.FromHexString(root.GetProperty("syntheticKeyHex").GetString()!);
        var hash = OperationFingerprint.ComputeChannel(key, root.GetProperty("actorId").GetGuid(), item.GetProperty("scopeId").GetGuid(),
            root.GetProperty("clientOperationId").GetGuid(), body.GetProperty("name").GetString()!, null, 1);
        Assert.Equal(item.GetProperty("expectedFingerprintHex").GetString(), Convert.ToHexStringLower(hash));
    }
    public static IEnumerable<object[]> Fixtures()
    {
        using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(CommunityMigrationTests.RepoRoot,"docs/fixtures/community-permissions.json")));
        return data.RootElement.GetProperty("cases").EnumerateArray().Select(item=>new object[]{item.GetProperty("id").GetString()!,item.GetRawText()}).ToArray();
    }
    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Evaluator_matches_published_view_and_management_policy(string id,string json)
    {
        using var data=JsonDocument.Parse(json);
        var settings=new JsonSerializerOptions(JsonSerializerOptions.Web);
        settings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var snapshot=data.RootElement.GetProperty("input").Deserialize<PermissionSnapshot>(settings)!;
        var result=PermissionEvaluator.Evaluate(snapshot);
        var expected=data.RootElement.GetProperty("expected");
        Assert.True(expected.GetProperty("canView").GetBoolean()==result.CanView,id);
        Assert.Equal(expected.GetProperty("canSendText").GetBoolean(),result.CanSendText);
        Assert.Equal(expected.GetProperty("canManageExistingChannel").GetBoolean(),result.CanManageExistingChannel);
        Assert.Equal(expected.GetProperty("managementPermissions").EnumerateArray().Select(item=>item.GetString()!).ToArray(),result.ManagementPermissions);
    }
    [Fact]
    public void Role_fingerprint_matches_published_order_independent_mask_and_scope()
    {
        using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(CommunityMigrationTests.RepoRoot,"docs/fixtures/community-operations.json")));
        var root=data.RootElement;
        Assert.Equal(root.GetProperty("permissionBitOrder").EnumerateArray().Select(item=>item.GetString()),ManagementPermissions.Codes);
        foreach(var item in root.GetProperty("cases").EnumerateArray().Where(item=>item.GetProperty("operationKind").GetString()=="create_role"))
        {
            var body=item.GetProperty("normalizedBody");
            var key=Convert.FromHexString(root.GetProperty("syntheticKeyHex").GetString()!);
            var actor=root.GetProperty("actorId").GetGuid();var server=item.GetProperty("scopeId").GetGuid();var operation=root.GetProperty("clientOperationId").GetGuid();
            var permissions=body.GetProperty("permissions").EnumerateArray().Select(value=>value.GetString()!).ToArray();
            var hash=OperationFingerprint.ComputeRole(key,actor,server,operation,body.GetProperty("name").GetString()!,permissions);
            Assert.Equal(item.GetProperty("expectedFingerprintHex").GetString(),Convert.ToHexStringLower(hash));
            Assert.NotEqual(hash,OperationFingerprint.ComputeRole(key,actor,Guid.CreateVersion7(),operation,body.GetProperty("name").GetString()!,permissions));
        }
        Assert.Empty(PermissionEvaluator.Management(true,false,[new(ViewEffect.Inherit,["manage_channels"],true),new(ViewEffect.Inherit,["manage_roles","channel_view"])]));
    }
}
