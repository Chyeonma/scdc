using Microsoft.EntityFrameworkCore;
using SCDC.Modules.Community.Features.Servers.Domain;
using SCDC.Modules.Community.Features.Memberships.Domain;
using SCDC.Modules.Community.Features.Permissions.Domain;
using SCDC.Modules.Community.Infrastructure.Idempotency;

namespace SCDC.Modules.Community.Infrastructure.Persistence;

internal sealed class CommunityDbContext(DbContextOptions<CommunityDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.HasDefaultSchema("community");
        model.Entity<Server>(b => { b.ToTable("servers"); b.HasKey(x => x.Id); });
        model.Entity<Membership>(b =>
        {
            b.ToTable("server_members");
            b.HasKey(x => new { x.ServerId, x.UserId });
            b.HasOne<Server>().WithMany().HasForeignKey(x => x.ServerId);
        });
        model.Entity<Role>(b =>
        {
            b.ToTable("roles");
            b.HasKey(x => x.Id);
            b.HasOne<Server>().WithMany().HasForeignKey(x => x.ServerId);
        });
        model.Entity<CommunityOperation>(b =>
        {
            b.ToTable("operations");
            b.HasKey(x => new { x.ActorUserId, x.Kind, x.ScopeId, x.ClientOperationId });
        });
        foreach (var entity in model.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
            {
                var name = string.Concat(property.Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
                property.SetColumnName(name);
            }
        model.Entity<Server>().Property(x => x.Id).ValueGeneratedNever();
        model.Entity<Role>().Property(x => x.Id).ValueGeneratedNever();
    }
}
