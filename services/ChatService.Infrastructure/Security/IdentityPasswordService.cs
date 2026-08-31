using ChatService.Domain.Entities;
using ChatService.Services.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace ChatService.Infrastructure.Security;

public sealed class IdentityPasswordService(IPasswordHasher<User> passwordHasher) : IPasswordService
{
    public string Hash(User user, string password) => passwordHasher.HashPassword(user, password);

    public PasswordCheckResult Verify(User user, string passwordHash, string password) =>
        passwordHasher.VerifyHashedPassword(user, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };

    public void PerformDummyHash(string password) => passwordHasher.HashPassword(new User(), password);
}
