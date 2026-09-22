using Microsoft.EntityFrameworkCore;
using SCDC.Contracts.Messaging;
using SCDC.Modules.Messaging.Domain;
using SCDC.Modules.Messaging.Infrastructure.Persistence;

namespace SCDC.Modules.Messaging.Infrastructure.Services;

internal sealed class ChannelSpaceProvisioner(MessagingDbContext dbContext, TimeProvider clock) : IChannelSpaceProvisioner
{
    public async Task<ChannelSpaceProvisionResult> CreateAsync(Guid createdByUserId, CancellationToken cancellationToken)
    {
        if (createdByUserId == Guid.Empty) return new(null, "Creator is required.");
        try
        {
            var now = clock.GetUtcNow();
            var id = Guid.CreateVersion7();
            dbContext.Spaces.Add(new ChatSpace { Id = id, SpaceType = SpaceType.Channel, Status = SpaceStatus.Active, CreatedByUserId = createdByUserId, CreatedAt = now, UpdatedAt = now, Version = 1 });
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(id);
        }
        catch (DbUpdateException) { return new(null, "The channel chat space could not be saved."); }
    }

    public async Task RetireAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var space = await dbContext.Spaces.SingleOrDefaultAsync(x => x.Id == spaceId, cancellationToken);
        if (space is null) return;
        space.Status = SpaceStatus.Deleted;
        space.DeletedAt = clock.GetUtcNow();
        space.UpdatedAt = space.DeletedAt.Value;
        space.Version++;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid spaceId, CancellationToken cancellationToken)
    {
        var space = await dbContext.Spaces.SingleOrDefaultAsync(x => x.Id == spaceId, cancellationToken);
        if (space is null || space.Status == SpaceStatus.Deleted) return;
        space.Status = SpaceStatus.Archived;
        space.ArchivedAt = clock.GetUtcNow();
        space.UpdatedAt = space.ArchivedAt.Value;
        space.Version++;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
