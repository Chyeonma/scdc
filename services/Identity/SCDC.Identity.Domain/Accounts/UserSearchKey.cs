using System.Text;

namespace SCDC.Modules.Identity.Domain;

internal static class UserSearchKey
{
    // No accent removal or culture-specific SQL case conversion.
    public static string Normalize(string value) => value.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
}
