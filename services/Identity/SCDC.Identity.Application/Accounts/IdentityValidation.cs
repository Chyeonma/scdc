using System.Net.Mail;
using System.Text.RegularExpressions;
using SCDC.BuildingBlocks.Application.Results;

namespace SCDC.Modules.Identity.Application;

internal static partial class IdentityValidation
{
    public static ValidationError? ValidateRegistration(RegisterUserCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        var username = command.Username.Trim();
        var displayName = command.DisplayName.Trim();

        if (!UsernamePattern().IsMatch(username))
        {
            errors["username"] = ["Username must contain 3-32 letters, numbers, underscores or dots."];
        }

        if (displayName.Length is < 1 or > 64)
        {
            errors["displayName"] = ["Display name must contain 1-64 characters."];
        }

        if (!IsValidEmail(command.Email))
        {
            errors["email"] = ["Email address is invalid."];
        }

        AddPasswordErrors(errors, "password", command.Password);
        return CreateValidationError(errors, "Identity.RegistrationInvalid", "Registration data is invalid.");
    }

    public static ValidationError? ValidatePassword(string field, string password)
    {
        var errors = new Dictionary<string, string[]>();
        AddPasswordErrors(errors, field, password);
        return CreateValidationError(errors, "Identity.PasswordInvalid", "The password does not meet the policy.");
    }

    public static ValidationError? ValidateProfile(UpdateProfileCommand command)
    {
        var errors = new Dictionary<string, string[]>();
        var displayName = command.DisplayName.Trim();

        if (displayName.Length is < 1 or > 64)
        {
            errors["displayName"] = ["Display name must contain 1-64 characters."];
        }

        if (command.Bio?.Length > 500)
        {
            errors["bio"] = ["Bio cannot exceed 500 characters."];
        }

        if (string.IsNullOrWhiteSpace(command.Locale) || command.Locale.Length > 16)
        {
            errors["locale"] = ["Locale must contain 1-16 characters."];
        }

        if (string.IsNullOrWhiteSpace(command.Timezone) || command.Timezone.Length > 64)
        {
            errors["timezone"] = ["Timezone must contain 1-64 characters."];
        }

        return CreateValidationError(errors, "Identity.ProfileInvalid", "Profile data is invalid.");
    }

    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            return false;
        }

        try
        {
            return new MailAddress(email.Trim()).Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void AddPasswordErrors(
        IDictionary<string, string[]> errors,
        string field,
        string password)
    {
        var messages = new List<string>();

        if (password.Length is < 8 or > 128)
        {
            messages.Add("Password must contain 8-128 characters.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            messages.Add("Password must contain at least one letter and one number.");
        }

        if (messages.Count > 0)
        {
            errors[field] = [.. messages];
        }
    }

    private static ValidationError? CreateValidationError(
        IReadOnlyDictionary<string, string[]> errors,
        string code,
        string description) => errors.Count == 0
            ? null
            : new ValidationError(code, description, errors);

    [GeneratedRegex("^[A-Za-z0-9_.]{3,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernamePattern();
}
