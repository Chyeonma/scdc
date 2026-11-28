using Grpc.Core;
using SCDC.Contracts.Identity;

namespace SCDC.BuildingBlocks.Infrastructure.Transport;

public static class ServiceRpc
{
    public static async Task<T> ReadAsync<T>(AsyncUnaryCall<T> call, CancellationToken ct, Func<Exception>? onConflict = null)
    {
        using (call)
        {
            try { return await call.ResponseAsync.WaitAsync(ct); }
            catch (RpcException error) when (error.StatusCode == StatusCode.AlreadyExists && onConflict is not null)
            { throw onConflict(); }
            catch (RpcException error)
            {
                var code = error.StatusCode == StatusCode.FailedPrecondition
                    ? RpcPeerGuard.PublicCode(error.Trailers.FirstOrDefault(entry => entry.Key == "scdc-error-code")?.Value)
                    : "ACCESS_CHECK_UNAVAILABLE";
                throw new AccessCheckException(code);
            }
        }
    }
    public static T Decode<T>(Func<T> decode)
    {
        try { return decode(); }
        catch (Exception error) when (error is FormatException or ArgumentException or OverflowException)
        { throw new AccessCheckException(); }
    }
}
