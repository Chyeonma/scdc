namespace SCDC.Modules.Identity.Application;

public interface IIdentityReadiness
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
