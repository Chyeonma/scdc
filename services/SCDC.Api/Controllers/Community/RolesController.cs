using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Api.Errors;
using SCDC.BuildingBlocks.Application.Results;
using SCDC.Contracts.Identity;
using SCDC.Modules.Community.Features.Permissions.Application;

namespace SCDC.Api.Controllers.Community;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateRoleRequest(Guid ClientOperationId,string? Name,JsonElement Permissions=default);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateRoleRequest(string? ExpectedVersion,JsonElement Name,JsonElement Permissions);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReplaceMemberRolesRequest(Guid MembershipId,string? ExpectedVersion,Guid[]? RoleIds);

[Authorize]
[Route("api/v1/servers/{serverId:guid}")]
public sealed class RolesController(IRoleService roles):ApiControllerBase
{
    private AccountActor Actor=>new(Claim("sub"),Claim("sid"),Claim("sst"));
    private Guid Claim(string name)=>Guid.TryParse(User.FindFirst(name)?.Value,out var id)?id:throw new InvalidOperationException($"Required claim '{name}' is missing.");
    [HttpGet("roles")]
    public async Task<ActionResult<RolePage>> List(Guid serverId,[FromQuery]int limit=20,[FromQuery]string? cursor=null,CancellationToken ct=default)
        =>FromResult(await roles.ListAsync(Actor,serverId,limit,cursor,ct));
    [HttpPost("roles")]
    [RequestSizeLimit(16_384)]
    public async Task<ActionResult<RoleView>> Create(Guid serverId,CreateRoleRequest request,CancellationToken ct)
    {
        if(request.Permissions.ValueKind!=JsonValueKind.Undefined && (request.Permissions.ValueKind!=JsonValueKind.Array
            ||request.Permissions.EnumerateArray().Any(item=>item.ValueKind!=JsonValueKind.String)))
            return ApiErrorMapper.ToObjectResult(new ValidationError("VALIDATION_FAILED","Invalid role data.",new Dictionary<string,string[]> {{"permissions",["Permissions must be an array of strings."]}}),HttpContext);
        var permissions=request.Permissions.ValueKind==JsonValueKind.Undefined?[]:request.Permissions.EnumerateArray().Select(item=>item.GetString()!).ToArray();
        var result=await roles.CreateAsync(Actor,serverId,new(request.ClientOperationId,request.Name,permissions),ct);
        if(result.IsFailure)return ApiErrorMapper.ToObjectResult(result.Error,HttpContext);
        Response.Headers.Location=$"/api/v1/servers/{serverId}/roles/{result.Value.Role.Id}";
        return result.Value.Created?StatusCode(201,result.Value.Role):Ok(result.Value.Role);
    }
    [HttpPatch("roles/{roleId:guid}")]
    [RequestSizeLimit(16_384)]
    public async Task<ActionResult<RoleView>> Update(Guid serverId,Guid roleId,UpdateRoleRequest request,CancellationToken ct)
    {
        var errors=new Dictionary<string,string[]>();
        var hasName=request.Name.ValueKind!=JsonValueKind.Undefined;
        var hasPermissions=request.Permissions.ValueKind!=JsonValueKind.Undefined;
        if(hasName&&request.Name.ValueKind!=JsonValueKind.String)errors["name"]=["Name must be a string."];
        if(hasPermissions&&(request.Permissions.ValueKind!=JsonValueKind.Array||request.Permissions.EnumerateArray().Any(item=>item.ValueKind!=JsonValueKind.String)))errors["permissions"]=["Permissions must be an array of strings."];
        if(errors.Count>0)return ApiErrorMapper.ToObjectResult(new ValidationError("VALIDATION_FAILED","Invalid role data.",errors),HttpContext);
        return FromResult(await roles.UpdateAsync(Actor,serverId,roleId,new(request.ExpectedVersion,hasName,hasName?request.Name.GetString():null,hasPermissions,hasPermissions?request.Permissions.EnumerateArray().Select(item=>item.GetString()!).ToArray():null),ct));
    }
    [HttpDelete("roles/{roleId:guid}")]
    public async Task<IActionResult> Delete(Guid serverId,Guid roleId,[FromQuery]string? expectedVersion,CancellationToken ct)
        =>FromNoContentResult(await roles.DeleteAsync(Actor,serverId,roleId,expectedVersion,ct));
    [HttpGet("members")]
    public async Task<ActionResult<MemberPage>> Members(Guid serverId,[FromQuery]int limit=20,[FromQuery]string? cursor=null,CancellationToken ct=default)
        =>FromResult(await roles.MembersAsync(Actor,serverId,limit,cursor,ct));
    [HttpGet("members/{userId:guid}/roles")]
    public async Task<ActionResult<MemberRoles>> MemberRoles(Guid serverId,Guid userId,CancellationToken ct)
        =>FromResult(await roles.MemberRolesAsync(Actor,serverId,userId,ct));
    [HttpPut("members/{userId:guid}/roles")]
    [RequestSizeLimit(16_384)]
    public async Task<ActionResult<MemberRoles>> Replace(Guid serverId,Guid userId,ReplaceMemberRolesRequest request,CancellationToken ct)
        =>FromResult(await roles.ReplaceAsync(Actor,serverId,userId,new(request.MembershipId,request.ExpectedVersion,request.RoleIds),ct));
}
