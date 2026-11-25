namespace SCDC.BuildingBlocks.Application.Results;

public enum ErrorType
{
    None = 0,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,
    ServiceUnavailable
}
