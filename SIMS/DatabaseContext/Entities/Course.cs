namespace SIMS.DatabaseContext.Entities
{
    public class Course
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public int Credits { get; set; }
        public string? Class { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int FacultyId { get; set; }
        public virtual Faculty Faculty { get; set; }

        public ICollection<FacultyCourse> FacultyCourses { get; set; } = new List<FacultyCourse>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    }
}
