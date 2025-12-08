using Microsoft.AspNetCore.Identity;

namespace SIMS.DatabaseContext.Entities
{
    public class Student
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Major { get; set; }
        public string? Gender { get; set; }
        public DateTime Dob { get; set; }

        // Academic
        public double GPA { get; set; }
        public string? AcademicStanding { get; set; }
        public string? Class { get; set; }
        public DateTime? CreateAt { get; set; } = DateTime.Now;
        public DateTime? UpdateAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    }
}
