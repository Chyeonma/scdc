using SCDC.Modules.Messaging.Hubs;

namespace SCDC.Api.Tests.Messaging;

public sealed class TypingStateRegistryTests
{
    [Fact]
    public void Typing_is_throttled_and_expires_without_a_stop_event()
    {
        var clock = new ManualTimeProvider();
        var states = new TypingStateRegistry(clock);
        var userId = Guid.NewGuid();
        var spaceId = Guid.NewGuid();

        Assert.True(states.Start("connection", userId, spaceId, out var firstExpiry));
        Assert.False(states.Start("connection", userId, spaceId, out _));
        clock.Advance(TimeSpan.FromSeconds(3));
        Assert.True(states.Start("connection", userId, spaceId, out var renewedExpiry));
        Assert.True(renewedExpiry > firstExpiry);

        clock.Advance(TimeSpan.FromSeconds(7));
        Assert.False(states.Stop("connection", userId, spaceId));
        Assert.True(states.Start("connection", userId, spaceId, out _));
        Assert.True(states.Stop("connection", userId, spaceId));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
