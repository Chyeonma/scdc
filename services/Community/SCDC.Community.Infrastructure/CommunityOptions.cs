namespace SCDC.Modules.Community.Infrastructure;

public sealed class CommunityOptions
{
    public const string SectionName = "Modules:Community";
    public string KeyRingPath { get; set; } = "";
    public OperationKeyOptions Operations { get; set; } = new();
}

public sealed class OperationKeyOptions
{
    public string ActiveKeyId { get; set; } = "";
    public Dictionary<string, string> Keys { get; set; } = new(StringComparer.Ordinal);

    internal byte[] GetKey(string id)
    {
        if (!Keys.TryGetValue(id, out var value) || id.Length is < 1 or > 100)
            throw new FingerprintKeyUnavailableException();
        try
        {
            var key = Convert.FromBase64String(value);
            return key.Length >= 32 ? key : throw new FingerprintKeyUnavailableException();
        }
        catch (FormatException) { throw new FingerprintKeyUnavailableException(); }
    }
    internal bool IsValid()
    {
        try
        {
            _ = GetKey(ActiveKeyId);
            foreach (var id in Keys.Keys)
                _ = GetKey(id);
            return true;
        }
        catch (FingerprintKeyUnavailableException) { return false; }
    }
}
internal sealed class FingerprintKeyUnavailableException : Exception;
