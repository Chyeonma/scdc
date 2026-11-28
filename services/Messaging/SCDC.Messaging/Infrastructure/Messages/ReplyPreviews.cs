using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Messaging.Application;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Messages;

internal static class ReplyPreviews
{
    public static Task<Dictionary<Guid, MessageReply>> ReadAsync(MessagingDbContext db, Guid spaceId, Guid[] ids, CancellationToken token) =>
        ids.Length == 0
            ? Task.FromResult(new Dictionary<Guid, MessageReply>())
            : Query(db, spaceId, ids).ToDictionaryAsync(reply => reply.Id, token);

    public static Task<MessageReply?> ReadAsync(MessagingDbContext db, Guid spaceId, Guid? id, CancellationToken token) =>
        id is Guid parent ? Query(db, spaceId, [parent]).SingleOrDefaultAsync(token) : Task.FromResult<MessageReply?>(null);

    private static IQueryable<MessageReply> Query(MessagingDbContext db, Guid spaceId, Guid[] ids) =>
        db.Messages.AsNoTracking().Where(m => m.SpaceId == spaceId && ids.Contains(m.Id))
            .Select(m => new MessageReply(m.Id, (Guid?)m.AuthorUserId ?? Guid.Empty,
                m.DeletedAt != null || m.Content == null ? null : m.Content.Length > 160 ? m.Content.Substring(0, 160) : m.Content,
                m.DeletedAt != null));
}
