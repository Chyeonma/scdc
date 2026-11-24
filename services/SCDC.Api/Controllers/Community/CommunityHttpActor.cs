using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SCDC.Contracts.Identity;

namespace SCDC.Api.Controllers.Community;

internal static class CommunityHttpActor
{
    public static AccountActor Read(ControllerBase controller,bool requireMembership=false)
    {
        var user=controller.User;
        Guid Claim(string name)=>Guid.Parse(user.FindFirst(name)!.Value);
        Guid? membership=null;
        if(requireMembership)
        {
            var values=controller.Request.Headers["X-SCDC-Membership-Id"];
            if(values.Count!=1||!Guid.TryParse(values[0],out var id)||id==Guid.Empty)
                throw new AccessCheckException("MEMBERSHIP_REQUIRED");
            membership=id;
        }
        var expires=DateTimeOffset.FromUnixTimeSeconds(long.Parse(user.FindFirst("exp")!.Value,CultureInfo.InvariantCulture));
        return new(Claim("sub"),Claim("sid"),Claim("sst"),expires,membership);
    }
}
