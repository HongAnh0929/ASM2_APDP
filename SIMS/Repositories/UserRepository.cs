using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using SIMS.Interfaces;

namespace SIMS.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SimDbContext _context;
        public UserRepository(SimDbContext dbContext)
        {
            _context = dbContext;
        }
        public async Task AddSync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id).AsTask();
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task SaveChangeAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
