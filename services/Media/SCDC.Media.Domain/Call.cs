using System.Globalization;

namespace SCDC.Media.Domain;

public sealed record PublicUser(Guid Id, string Username, string DisplayName);
// AccessToken is held only in the authenticated connection registry, never persisted or broadcast.
public sealed record MediaActor(Guid UserId, Guid SessionId, Guid InstanceId, string AccessToken,
    PublicUser User, DateTimeOffset ExpiresAt);
public sealed record CallRecord(Guid Id, Guid CallerId, Guid PeerId, Guid CallerSessionId,
    Guid CallerInstanceId, Guid? PeerSessionId, Guid? PeerInstanceId, string CallerName,
    string PeerName, string Status, string? EndReason, long Version, string RoomName,
    DateTimeOffset CreatedAt, DateTimeOffset RingExpiresAt, bool ProviderClosed, DateTimeOffset UpdatedAt)
{
    public bool Contains(Guid user) => CallerId == user || PeerId == user;
    public bool Owns(MediaActor actor) => actor.UserId == CallerId
        ? actor.SessionId == CallerSessionId && actor.InstanceId == CallerInstanceId
        : actor.UserId == PeerId && actor.SessionId == PeerSessionId && actor.InstanceId == PeerInstanceId;
    public CallSnapshot Snapshot(MediaActor actor) => new(Id, CallerId, PeerId, CallerName, PeerName,
        Status, EndReason, Version.ToString(CultureInfo.InvariantCulture), RingExpiresAt, Owns(actor));
}
public sealed record CallSnapshot(Guid Id, Guid CallerUserId, Guid PeerUserId, string CallerName,
    string PeerName, string Status, string? EndReason, string Version, DateTimeOffset RingExpiresAt, bool OwnsCall);
public sealed class MediaProblem(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public static class CallRules
{
    public static long Version(string? text) => long.TryParse(text, NumberStyles.None,
        CultureInfo.InvariantCulture, out var value) && value > 0 && text == value.ToString(CultureInfo.InvariantCulture)
        ? value : throw new MediaProblem(400, "Media.ValidationFailed", "A canonical positive version string is required.");
    public static void Uuid4(Guid id)
    {
        if (id.Version != 4 || (id.ToByteArray(true)[8] & 0xc0) != 0x80)
            throw new MediaProblem(400, "Media.ValidationFailed", "A UUIDv4 is required.");
    }
    public static void Action(CallRecord call, MediaActor actor, string action, string? expected, DateTimeOffset now)
    {
        if (!call.Contains(actor.UserId)) throw new MediaProblem(404, "Media.NotFound", "Call unavailable.");
        if (Version(expected) != call.Version) throw new MediaProblem(409, "Media.VersionConflict", "Reload the current call.");
        if (call.Status == "ended" || call.Status == "ringing" && now >= call.RingExpiresAt)
            throw new MediaProblem(409, "Media.Expired", "Call has ended or expired.");
        var allowed = action switch
        {
            "accept" or "reject" => call.Status == "ringing" && actor.UserId == call.PeerId,
            "cancel" => call.Status == "ringing" && actor.UserId == call.CallerId && call.Owns(actor),
            "end" => call.Status == "active" && call.Owns(actor),
            _ => false
        };
        if (!allowed) throw new MediaProblem(409, "Media.InvalidState", "This tab cannot perform that call action.");
    }
}
