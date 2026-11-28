using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Access;

// Call inside the owner transaction. Membership changes must take the same space lock.
internal sealed class PrivateConversationMembership(MessagingDbContext db)
{
    public async Task<Access?> LockAsync(Guid actor, Guid id, ConversationKind? kind, bool write, CancellationToken token)
    {
        var spaces = db.Spaces.Where(s => s.Id == id && s.Status != 3 && s.DeletedAt == null);
        if (kind is ConversationKind requested)
            spaces = spaces.Where(s => s.SpaceType == (short)requested);
        var type = await spaces.Select(s => (short?)s.SpaceType).SingleOrDefaultAsync(token);
        var members = db.SpaceMembers.AsNoTracking().Where(m => m.SpaceId == id && m.MembershipStatus == 1 && m.LeftAt == null);
        DirectConversation? pair = null;
        if (type == (short)ConversationKind.Direct)
        {
            pair = await db.DirectConversations.AsNoTracking().SingleOrDefaultAsync(p => p.SpaceId == id, token);
            if (pair is null || pair.UserLowId != actor && pair.UserHighId != actor) return null;
        }
        else if (type != (short)ConversationKind.Group || !await members.AnyAsync(m => m.UserId == actor, token))
        {
            return null;
        }

        if (write)
            await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={id} FOR UPDATE").ToListAsync(token);
        else
            await db.Database.SqlQuery<Guid>($"SELECT id AS \"Value\" FROM messaging.spaces WHERE id={id} FOR SHARE").ToListAsync(token);

        var space = await (write ? spaces : spaces.AsNoTracking()).SingleOrDefaultAsync(s => s.SpaceType == type, token);
        if (space is null) return null;
        if (pair is null)
        {
            return await db.GroupConversations.AnyAsync(g => g.SpaceId == id, token)
                && await members.AnyAsync(m => m.UserId == actor, token) ? new Access(space, null) : null;
        }

        var active = await members.Select(m => m.UserId).ToListAsync(token);
        return active.Count == 2 && active.Contains(pair.UserLowId) && active.Contains(pair.UserHighId)
            ? new Access(space, pair.UserLowId == actor ? pair.UserHighId : pair.UserLowId) : null;
    }

    internal sealed record Access(ChatSpace Space, Guid? PeerId);
}
