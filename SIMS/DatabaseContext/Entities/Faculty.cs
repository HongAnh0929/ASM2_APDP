using System.ComponentModel.DataAnnotations;

namespace SIMS.DatabaseContext.Entities
{
    public class Faculty
    {
        public int FacultyId { get; set; }
        public string FacultyName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public DateTime? HireDate { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public ICollection<FacultyCourse> FacultyCourses { get; set; } = new List<FacultyCourse>();
    }
}
