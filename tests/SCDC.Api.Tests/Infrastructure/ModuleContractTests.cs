using SCDC.Contracts.Identity;

namespace SCDC.Api.Tests.Infrastructure;

public sealed class ModuleContractTests
{
    [Fact]
    public void Contracts_do_not_reference_database_libraries_or_module_implementations()
    {
        var references = typeof(IIdentityPermitIssuer).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference => reference.Name is not null &&
            (reference.Name.StartsWith("System.Data", StringComparison.Ordinal)
                || reference.Name.StartsWith("Npgsql", StringComparison.Ordinal)
                || reference.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || reference.Name.StartsWith("SCDC.Modules", StringComparison.Ordinal)
                || reference.Name == "SCDC.BuildingBlocks"));
    }

    [Fact]
    public void Every_module_contract_call_is_asynchronous_and_accepts_cancellation()
    {
        var calls = typeof(IIdentityPermitIssuer).Assembly.GetExportedTypes()
            .Where(type => type.IsInterface).SelectMany(type => type.GetMethods()).ToArray();
        Assert.NotEmpty(calls);
        Assert.All(calls, call =>
        {
            if (call.IsSpecialName) return; // Owner identifiers are local metadata.
            Assert.True(typeof(Task).IsAssignableFrom(call.ReturnType), call.ToString());
            Assert.Contains(call.GetParameters(), parameter => parameter.ParameterType == typeof(CancellationToken));
        });
    }
}
