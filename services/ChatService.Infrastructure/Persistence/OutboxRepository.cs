using ChatService.Data;
using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;

namespace ChatService.Infrastructure.Persistence;

public sealed class OutboxRepository(ChatDbContext dbContext) : IOutboxRepository
{
    public void Add(OutboxEvent outboxEvent) => dbContext.OutboxEvents.Add(outboxEvent);
}
