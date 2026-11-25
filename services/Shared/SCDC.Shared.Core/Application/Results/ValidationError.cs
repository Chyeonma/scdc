namespace SCDC.BuildingBlocks.Application.Results;

public sealed record ValidationError(
    string Code,
    string Description,
    IReadOnlyDictionary<string, string[]> Errors)
    : Error(Code, Description, ErrorType.Validation);
