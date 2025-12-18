using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;

namespace SIMS.Controllers
{
    public class SearchController : Controller
    {
        private readonly SimDbContext _db;

        public SearchController(SimDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return RedirectToAction("Index", "Dashboard");

            q = q.ToLower().Trim();

            // ===== QUICK COMMAND =====
            if (q.Contains("list students") || q == "students")
                return RedirectToAction("Index", "Students");

            if (q.Contains("add student"))
                return RedirectToAction("Add", "Students");

            if (q.Contains("list courses") || q == "courses")
                return RedirectToAction("Index", "Courses");

            if (q.Contains("add course"))
                return RedirectToAction("Add", "Courses");

            if (q.Contains("list schedules") || q == "schedules")
                return RedirectToAction("Index", "Schedules");

            if (q.Contains("add schedule"))
                return RedirectToAction("Add", "Schedules");

            // =========================
            // STUDENTS
            // =========================
            var students = _db.Students
                .Include(s => s.User)
                .Where(s =>
                    (!string.IsNullOrEmpty(s.FullName) && s.FullName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.Class) && s.Class.ToLower().Contains(q))
                )
                .ToList();

            // =========================
            // COURSES
            // =========================
            var courses = _db.Courses
                .Include(c => c.Faculty)
                .Where(c =>
                    (!string.IsNullOrEmpty(c.CourseName) && c.CourseName.ToLower().Contains(q)) ||
                    (!string.IsNullOrEmpty(c.Class) && c.Class.ToLower().Contains(q))
                )
                .ToList();

            // =========================
            // SCHEDULES
            // =========================
            var schedules = _db.Schedules
                .Include(s => s.Course)
                .Include(s => s.Faculty)
                .Where(s =>
                    (!string.IsNullOrEmpty(s.Room) && s.Room.ToLower().Contains(q)) ||

                    // search theo giờ (08, 09, 10...)
                    (s.StartTime.HasValue && s.StartTime.Value.ToString(@"hh\:mm").Contains(q)) ||
                    (s.EndTime.HasValue && s.EndTime.Value.ToString(@"hh\:mm").Contains(q))
                )
                .ToList();

            ViewBag.Query = q;
            ViewBag.Students = students;
            ViewBag.Courses = courses;
            ViewBag.Schedules = schedules;

            return View();
        }
    }
}
