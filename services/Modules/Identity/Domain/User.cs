namespace SCDC.Modules.Identity.Domain;

internal sealed class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public string NormalizedUsername { get; private set; } = string.Empty;
    public UserStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public int Version { get; set; }

    public UserProfile? Profile { get; set; }
    public PasswordCredential? PasswordCredential { get; set; }
    public UserSecurityState? SecurityState { get; set; }
    public ICollection<UserEmail> Emails { get; } = new List<UserEmail>();
    public ICollection<AuthSession> Sessions { get; } = new List<AuthSession>();
    public ICollection<AccountToken> AccountTokens { get; } = new List<AccountToken>();
}
