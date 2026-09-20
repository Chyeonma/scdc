namespace SCDC.Contracts.Identity;

public interface IIdentitySessionValidator
{
    Task<IdentitySessionValidation> ValidateAsync(
        Guid userId,
        Guid sessionId,
        Guid securityStamp,
        CancellationToken cancellationToken);
}

public sealed record IdentitySessionValidation(bool IsValid)
{
    public static IdentitySessionValidation Valid { get; } = new(true);
    public static IdentitySessionValidation Invalid { get; } = new(false);
}
