using SIMS.DatabaseContext.Entities;
using SIMS.Interfaces;

namespace SIMS.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        public UserService(IUserRepository repository)
        {
            _userRepository = repository;
        }

        public async Task<User?> LoginUserAsync(string username, string password)
        {
            var user = await _userRepository.GetUserByUsername(username); // kiem tra username
            if (user == null) return null;

            return user.HashPassword.Equals(password) ? user : null; // kiem tra mat khau
        }
    }
}
