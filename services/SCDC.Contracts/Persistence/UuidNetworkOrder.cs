namespace SCDC.Contracts.Persistence;

public static class UuidNetworkOrder
{
    public static int Compare(Guid left, Guid right)
    {
        Span<byte> leftBytes = stackalloc byte[16];
        Span<byte> rightBytes = stackalloc byte[16];
        left.TryWriteBytes(leftBytes, bigEndian: true, out _);
        right.TryWriteBytes(rightBytes, bigEndian: true, out _);
        return leftBytes.SequenceCompareTo(rightBytes);
    }

    public static (Guid Low, Guid High) Pair(Guid left, Guid right) =>
        Compare(left, right) < 0 ? (left, right) : (right, left);
}
