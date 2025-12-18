using System.ComponentModel.DataAnnotations;

namespace SIMS.DatabaseContext.Entities
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required]
        public string CourseName { get; set; } = null!;

        [Range(1, 10)]
        public int Credits { get; set; }

        public string? Class { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // ✅ Faculty bắt buộc
        public int FacultyId { get; set; }
        public Faculty? Faculty { get; set; }

        public ICollection<FacultyCourse> FacultyCourses { get; set; } = new List<FacultyCourse>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
