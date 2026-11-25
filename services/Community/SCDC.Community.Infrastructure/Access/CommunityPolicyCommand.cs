using System.Text.Json;
using SCDC.Contracts.Identity;

namespace SCDC.Modules.Community.Infrastructure.Access;

internal sealed record CommunityPolicyCommand(string Kind, Guid ResourceId, JsonElement Data)
{
    public static CommunityPolicyCommand Create<T>(string kind,Guid resource,T value)=>new(kind,resource,JsonSerializer.SerializeToElement(value));
    public T Read<T>()=>Data.Deserialize<T>()!;
}
internal sealed record CommunityPolicyWork(Guid OperationId,Guid ServerId,Guid ActorId,Guid SessionId,
    Guid MembershipId,CommunityPolicyCommand Command,SCDC.Contracts.Community.CommunityScope[] Scopes);
