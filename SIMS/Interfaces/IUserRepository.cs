using SIMS.DatabaseContext.Entities;

namespace SIMS.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsername(string username);
        Task<User?> GetUserById(int id);
        Task AddSync(User user); // tao moi tai khoan
        Task SaveChangeAsync(); // luu du lieu xuong csdl
    }
}
