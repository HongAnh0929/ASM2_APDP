namespace SIMS.DatabaseContext.Entities
{
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime Date { get; set; }
        public bool Present { get; set; }
        public int FacultyId { get; set; }

        public Student? Student { get; set; }
        public Course? Course { get; set; }
        public Faculty? Faculty { get; set; }

    }
}
