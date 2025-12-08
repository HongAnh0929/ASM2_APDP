namespace SIMS.DatabaseContext.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string HashPassword { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string Role { get; set; } = "Admin";
        public byte Status { get; set; } = 1;
        public DateTime? CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; } = DateTime.Now;

        public Student? Student { get; set; }
        public Faculty? Faculty { get; set; }
    }
}
