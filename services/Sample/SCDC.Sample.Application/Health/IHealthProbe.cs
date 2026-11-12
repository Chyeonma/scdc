using SCDC.Sample.Domain.Health;

namespace SCDC.Sample.Application.Health;

public interface IHealthProbe
{
    ServiceHealth Check();
}
