namespace SCDC.Modules.Messaging.Application;

public interface IFileScanner
{
    Task<bool> IsCleanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
}
