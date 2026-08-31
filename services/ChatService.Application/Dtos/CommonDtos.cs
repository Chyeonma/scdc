namespace ChatService.Dtos;

public sealed record ItemsResponse<T>(IReadOnlyList<T> Items);
