namespace SCDC.Modules.Identity.Domain;

internal sealed class UserProfile
{
    public Guid UserId { get; set; }
    public required string DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarObjectKey { get; set; }
    public required string Locale { get; set; }
    public required string Timezone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}
