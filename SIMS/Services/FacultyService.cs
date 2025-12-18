using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;

namespace SIMS.Services
{
    public class FacultyService
    {
        private readonly SimDbContext _db;

        public FacultyService(SimDbContext db)
        {
            _db = db;
        }

        // Admin
        public async Task<List<Faculty>> GetAllFacultyAsync()
        {
            return await _db.Faculties
                .Include(f => f.User)
                .ToListAsync();
        }

        // Faculty detail + assigned courses
        public async Task<Faculty?> GetFacultyDetailAsync(int facultyId)
        {
            return await _db.Faculties
                .Include(f => f.User)
                .Include(f => f.FacultyCourses)
                    .ThenInclude(fc => fc.Course)
                .FirstOrDefaultAsync(f => f.FacultyId == facultyId);
        }

        // Attendance
        public async Task<List<Student>> GetStudentsByCourseAsync(int courseId)
        {
            return await _db.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.Student)
                    .ThenInclude(s => s.User)
                .Select(e => e.Student)
                .ToListAsync();
        }
    }
}
