using SCDC.Media.Domain;

namespace SCDC.Media.Tests;

public sealed class CallRulesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static MediaActor Actor() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "not-a-token", new(Guid.NewGuid(), "test", "Test"), Now.AddMinutes(5));
    private readonly MediaActor caller = Actor(), peer = Actor();
    private CallRecord Ring() => new(Guid.CreateVersion7(), caller.UserId, peer.UserId, caller.SessionId, caller.InstanceId,
        null, null, "Caller", "Peer", "ringing", null, 1, "test-room", Now, Now.AddSeconds(30), false, Now);
    [Fact] public void Only_recipient_can_accept()
    {
        CallRules.Action(Ring(), peer, "accept", "1", Now);
        Assert.Equal("Media.InvalidState", Assert.Throws<MediaProblem>(() => CallRules.Action(Ring(), caller, "accept", "1", Now)).Code);
    }
    [Fact] public void Another_recipient_tab_can_accept_but_cannot_own_caller()
    {
        CallRules.Action(Ring(), peer with { InstanceId = Guid.NewGuid() }, "accept", "1", Now);
        Assert.False(Ring().Owns(caller with { InstanceId = Guid.NewGuid() }));
    }
    [Fact] public void Caller_cancel_is_bound_to_session_and_instance()
    {
        CallRules.Action(Ring(), caller, "cancel", "1", Now);
        Assert.Throws<MediaProblem>(() => CallRules.Action(Ring(), caller with { SessionId = Guid.NewGuid() }, "cancel", "1", Now));
        Assert.Throws<MediaProblem>(() => CallRules.Action(Ring(), caller with { InstanceId = Guid.NewGuid() }, "cancel", "1", Now));
    }
    [Fact] public void Stale_version_cannot_win_accept_race()
    {
        var active = Ring() with { Status = "active", Version = 2, PeerSessionId = peer.SessionId, PeerInstanceId = peer.InstanceId };
        Assert.Equal("Media.VersionConflict", Assert.Throws<MediaProblem>(() => CallRules.Action(active, peer, "accept", "1", Now)).Code);
        CallRules.Action(active, peer, "end", "2", Now);
        Assert.Throws<MediaProblem>(() => CallRules.Action(active, peer with { InstanceId = Guid.NewGuid() }, "end", "2", Now));
    }
    [Fact] public void Ring_deadline_is_exclusive_and_ended_call_cannot_resurrect()
    {
        Assert.Equal("Media.Expired", Assert.Throws<MediaProblem>(() => CallRules.Action(Ring(), peer, "accept", "1", Now.AddSeconds(30))).Code);
        Assert.Throws<MediaProblem>(() => CallRules.Action(Ring() with { Status = "ended" }, peer, "accept", "1", Now));
    }
    [Fact] public void Outsiders_get_no_call_metadata()
    {
        Assert.Equal(404, Assert.Throws<MediaProblem>(() => CallRules.Action(Ring(), Actor(), "end", "1", Now)).Status);
    }
    [Theory] [InlineData("01")] [InlineData("0")] [InlineData("+1")] [InlineData(" 1")] [InlineData("1.0")] [InlineData("9223372036854775808")] [InlineData(null)]
    public void Version_wire_format_rejects_ambiguous_values(string? text) => Assert.Throws<MediaProblem>(() => CallRules.Version(text));
    [Fact] public void Versions_keep_int64_precision() => Assert.Equal(long.MaxValue, CallRules.Version("9223372036854775807"));
    [Fact] public void Operation_and_instance_ids_must_be_random_uuid4()
    {
        CallRules.Uuid4(Guid.NewGuid());
        Assert.Throws<MediaProblem>(() => CallRules.Uuid4(Guid.Empty));
        Assert.Throws<MediaProblem>(() => CallRules.Uuid4(Guid.CreateVersion7()));
    }
}
