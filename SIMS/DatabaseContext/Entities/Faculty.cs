namespace SIMS.DatabaseContext.Entities
{
    public class Faculty
    {
        public int FacultyId { get; set; }
        public string FacultyName { get; set; }  
        public string Email { get; set; }
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public DateTime? HireDate { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public ICollection<FacultyCourse> FacultyCourses { get; set; } = new List<FacultyCourse>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    }
}
