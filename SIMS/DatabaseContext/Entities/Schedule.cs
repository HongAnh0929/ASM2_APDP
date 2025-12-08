namespace SIMS.DatabaseContext.Entities
{
    public class Schedule
    {
        public int ScheduleId { get; set; }
        public int CourseId { get; set; }
        public int FacultyId { get; set; }
        public DateTime Date { get; set; }
        public string? Class { get; set; }
        public string? Time { get; set; }    // Ví dụ: "08:00 - 10:00"
        public string? Room { get; set; }    // Phòng thay đổi mỗi ngày

        public Course? Course { get; set; }
        public Faculty? Faculty { get; set; }
    }
}
