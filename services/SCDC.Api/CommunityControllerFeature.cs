using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SCDC.Api;

internal sealed class CommunityControllerFeature : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var controller in feature.Controllers.Where(type => type.Namespace == "SCDC.Api.Controllers.Community").ToArray())
            feature.Controllers.Remove(controller);
    }
}
