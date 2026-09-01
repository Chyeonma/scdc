using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Api.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_contains_value_and_no_error()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_contains_error_and_hides_value()
    {
        var error = Error.NotFound("Identity.UserNotFound", "User was not found.");
        var result = Result.Failure<string>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
