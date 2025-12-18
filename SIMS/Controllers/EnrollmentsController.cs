using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EnrollmentsController : Controller
    {
        private readonly SimDbContext _db;

        public EnrollmentsController(SimDbContext db)
        {
            _db = db;
        }

        // ================= AUTO ENROLL BY CLASS =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AutoEnrollByClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                TempData["ErrorMessage"] = "Class name is required.";
                return RedirectToAction("Index", "Students");
            }

            var students = _db.Students
                .Where(s => s.Class == className)
                .ToList();

            var courses = _db.Courses
                .Where(c => c.Class == className)
                .ToList();

            foreach (var student in students)
            {
                foreach (var course in courses)
                {
                    bool exists = _db.Enrollments.Any(e =>
                        e.StudentId == student.StudentId &&
                        e.CourseId == course.CourseId);

                    if (!exists)
                    {
                        _db.Enrollments.Add(new Enrollment
                        {
                            StudentId = student.StudentId,
                            CourseId = course.CourseId
                        });
                    }
                }
            }

            _db.SaveChanges();

            TempData["SuccessMessage"] =
                $"Auto-enrolled all students and courses of class {className}.";

            return RedirectToAction("Index", "Students");
        }
    }
}
