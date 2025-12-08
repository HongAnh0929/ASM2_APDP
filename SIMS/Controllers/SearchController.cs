using Microsoft.AspNetCore.Mvc;
using SIMS.DatabaseContext;
using Microsoft.EntityFrameworkCore;

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

            q = q.ToLower();

            if (q.Contains("list students") || q.Contains("students"))
                return RedirectToAction("Index", "Students");

            if (q.Contains("add student"))
                return RedirectToAction("Add", "Students");

            if (q.Contains("list courses") || q.Contains("courses"))
                return RedirectToAction("Index", "Courses");

            if (q.Contains("add course"))
                return RedirectToAction("Add", "Courses");

            if (q.Contains("list schedules") || q.Contains("schedules"))
                return RedirectToAction("Index", "Schedules");

            if (q.Contains("add schedule"))
                return RedirectToAction("Add", "Schedules");

            // Students
            var students = _db.Students
                .Where(s => s.FullName.Contains(q) || s.Class.Contains(q))
                .Include(s => s.User)
                .ToList();

            // Courses
            var courses = _db.Courses
                .Where(c => c.CourseName.Contains(q) || c.Class.Contains(q))
                .Include(c => c.Faculty)
                .ToList();

            // Schedules
            var schedules = _db.Schedules
                .Where(s => s.Room.Contains(q) || s.Time.Contains(q))
                .Include(s => s.Course)
                .Include(s => s.Faculty)
                .ToList();

            // Truyền qua View
            ViewBag.Query = q;
            ViewBag.Students = students;
            ViewBag.Courses = courses;
            ViewBag.Schedules = schedules;

            return View();
        }
    }
}
