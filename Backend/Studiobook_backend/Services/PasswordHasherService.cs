using Microsoft.AspNetCore.Identity;
using Studiobook_backend.Entities;
using Studiobook_backend.Services.Interfaces;

namespace Studiobook_backend.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        public string Hash(string password)
        {
            var dummyUser = new User();
            return _passwordHasher.HashPassword(dummyUser, password);
        }

        public bool Verify(string hashedPassword, string inputPassword)
        {
            var dummyUser = new User();
            var result = _passwordHasher.VerifyHashedPassword(dummyUser, hashedPassword, inputPassword);

            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
