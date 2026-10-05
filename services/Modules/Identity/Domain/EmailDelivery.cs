namespace SCDC.Modules.Identity.Domain;

internal enum EmailDeliveryStatus : short
{
    Pending,
    Sending,
    RetryPending,
    ProviderAccepted,
    Failed,
    Suppressed
}

internal sealed class EmailDelivery
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AccountTokenId { get; set; }
    public Guid OutboxEventId { get; set; }
    public AccountTokenPurpose Purpose { get; set; }
    public required string Recipient { get; set; }
    public int TemplateVersion { get; set; } = 1;
    public string? ProtectedEnvelope { get; set; }
    public DateTimeOffset EnvelopeExpiresAt { get; set; }
    public EmailDeliveryStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? TerminalAt { get; set; }
}
