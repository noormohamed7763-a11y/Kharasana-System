using Microsoft.AspNetCore.Identity;
using Kharasana.Application.Interfaces.Services;
using Kharasana.Domain.Entities;

namespace Kharasana.Tests
{
    public class PasswordHasherAdapter : IPasswordHasher
    {
        private readonly IPasswordHasher<User> _inner;

        public PasswordHasherAdapter(IPasswordHasher<User> inner)
        {
            _inner = inner;
        }

        public string Hash(string password)
        {
            // We don't have a User instance here; create a temporary one for hashing.
            var tempUser = new User();
            return _inner.HashPassword(tempUser, password);
        }

        public bool Verify(string password, string hash)
        {
            var tempUser = new User();
            var result = _inner.VerifyHashedPassword(tempUser, hash, password);
            return result == PasswordVerificationResult.Success;
        }

        public void VerifyDummy(string password)
        {
            // Perform a dummy verification to match timing expectations.
            var tempUser = new User();
            _ = _inner.VerifyHashedPassword(tempUser, string.Empty, password);
        }
    }
}
