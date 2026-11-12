using SCDC.Sample.Application.Health;
using SCDC.Sample.Domain.Health;

namespace SCDC.Sample.Infrastructure.Health;

// Liveness of this process only; the sample has no external dependencies.
internal sealed class ProcessHealthProbe(TimeProvider clock) : IHealthProbe
{
    public ServiceHealth Check() => new(true, clock.GetUtcNow());
}
