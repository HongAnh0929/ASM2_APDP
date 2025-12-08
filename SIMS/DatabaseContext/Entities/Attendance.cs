namespace SIMS.DatabaseContext.Entities
{
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime Date { get; set; }
        public bool Present { get; set; }
        public int? FacultyId { get; set; }

        public Student Student { get; set; } = null!;
        public Course Course { get; set; } = null!;
        public Faculty? Faculty { get; set; }

    }
}
