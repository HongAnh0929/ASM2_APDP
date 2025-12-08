using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using Microsoft.EntityFrameworkCore;

namespace SIMS.Services
{
    public class FacultyService
    {
        private readonly SimDbContext _db; // DbContext dùng để truy cập database

        public FacultyService(SimDbContext db)
        {
            _db = db; // Inject DbContext qua constructor
        }

        // Lấy toàn bộ Faculty kèm User
        public async Task<List<Faculty>> GetAllFaculty()
        {
            return await _db.Faculties
                .Include(f => f.User) // Join thêm User
                .ToListAsync();
        }

        // Lấy 1 Faculty theo FacultyId
        public async Task<Faculty?> GetByFacultyId(int facultyId)
        {
            return await _db.Faculties
                .Include(f => f.User) // Lấy User liên kết
                .Include(f => f.FacultyCourses) // Lấy danh sách FacultyCourses
                    .ThenInclude(fc => fc.Course) // Lấy luôn Course cho mỗi FacultyCourse
                .FirstOrDefaultAsync(f => f.FacultyId == facultyId);
        }

        // Lưu điểm danh
        public async Task RecordAttendance(int facultyId, int courseId, DateTime date, Dictionary<int, bool> attendanceDict)
        {
            foreach (var kvp in attendanceDict) // Duyệt qua từng student
            {
                int studentId = kvp.Key;
                bool Present = kvp.Value;

                // Kiểm tra xem đã có Attendance record chưa
                var existing = await _db.Attendances
                    .FirstOrDefaultAsync(a => a.FacultyId == facultyId
                                           && a.CourseId == courseId
                                           && a.StudentId == studentId
                                           && a.Date == date);

                if (existing != null)
                {
                    // Nếu tồn tại -> update
                    existing.Present = Present;
                }
                else
                {
                    // Nếu chưa -> tạo mới
                    _db.Attendances.Add(new Attendance
                    {
                        FacultyId = facultyId,
                        CourseId = courseId,
                        StudentId = studentId,
                        Date = date,
                        Present = Present
                    });
                }
            }

            await _db.SaveChangesAsync(); // Lưu tất cả thay đổi vào DB
        }

        // Lấy danh sách Student thuộc 1 course
        public async Task<List<Student>> GetStudentsByCourse(int courseId)
        {
            return await _db.Enrollments
                .Where(e => e.CourseId == courseId) // Chỉ lấy enrollments của course
                .Include(e => e.Student)            // Join sang Student
                .Select(e => e.Student)             // Lấy Student từ từng Enrollment
                .ToListAsync();
        }

        // Lấy toàn bộ học sinh
        public async Task<List<Student>> GetAllStudents()
        {
            return await _db.Students
                .Include(s => s.User) // Join User để xem info đăng nhập
                .ToListAsync();
        }
    }
}
