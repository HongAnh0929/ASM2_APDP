using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System.Security.Claims;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class SchedulesController : Controller
    {
        private readonly SimDbContext _db;

        public SchedulesController(SimDbContext db)
        {
            _db = db;
        }

        // ================= LIST =================
        public IActionResult Index(int? courseId = null)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var schedulesQuery = _db.Schedules
                .Include(s => s.Course)
                .Include(s => s.Faculty)
                .AsQueryable();

            // Role-based filtering
            if (role == "Faculty")
            {
                // Lấy FacultyId
                var facultyId = _db.Faculties
                    .Where(f => f.UserId == userId)
                    .Select(f => f.FacultyId)
                    .FirstOrDefault();

                schedulesQuery = schedulesQuery.Where(s => s.FacultyId == facultyId);
            }
            else if (role == "Student")
            {
                // Lấy Class của student
                var studentClass = _db.Students
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Class)
                    .FirstOrDefault()?.Trim();

                if (!string.IsNullOrEmpty(studentClass))
                {
                    // Chuyển sang LINQ-to-Objects để xử lý Split
                    schedulesQuery = schedulesQuery
                        .Where(s => s.Class != null)
                        .AsEnumerable() // EF không hiểu Split/Trim
                        .Where(s =>
                            s.Class.Split(',')       // Split nhiều lớp
                                   .Select(cl => cl.Trim()) // Trim khoảng trắng
                                   .Contains(studentClass))
                        .AsQueryable();
                }
                else
                {
                    // Nếu Student chưa có class
                    schedulesQuery = Enumerable.Empty<Schedule>().AsQueryable();
                }
            }

            // Nếu courseId được truyền vào, lọc theo course
            if (courseId.HasValue)
            {
                schedulesQuery = schedulesQuery.Where(s => s.CourseId == courseId.Value);
            }

            // Lấy danh sách và trả về View
            var schedules = schedulesQuery
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToList();

            // Lưu tên course vào ViewBag (nếu cần hiển thị)
            if (courseId.HasValue)
            {
                ViewBag.CourseName = _db.Courses
                    .Where(c => c.CourseId == courseId.Value)
                    .Select(c => c.CourseName)
                    .FirstOrDefault();
            }

            return View(schedules);
        }

        // ================= ADD (GET) =================
        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            BuildDropdowns();
            return View(new Schedule { Date = DateTime.Today });
        }

        // ================= ADD (POST) =================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Schedule model)
        {
            if (model.Date < DateTime.Today)
                ModelState.AddModelError("Date", "Date cannot be in the past.");

            if (!model.StartTime.HasValue || !model.EndTime.HasValue)
                ModelState.AddModelError("", "Start time and End time are required.");
            else if (model.StartTime >= model.EndTime)
                ModelState.AddModelError("", "End time must be later than Start time.");

            bool conflict = await _db.Schedules.AnyAsync(s =>
                s.CourseId == model.CourseId &&
                s.Class == model.Class &&
                s.Date == model.Date &&
                model.StartTime < s.EndTime &&
                model.EndTime > s.StartTime);

            if (conflict)
                ModelState.AddModelError("", "Schedule time conflicts with existing schedule.");

            if (!ModelState.IsValid)
            {
                BuildDropdowns(model.FacultyId, model.CourseId);
                return View(model);
            }

            _db.Schedules.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.Schedules.FindAsync(id);
            if (model == null) return NotFound();

            BuildDropdowns(model.FacultyId, model.CourseId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Schedule model)
        {
            var existing = await _db.Schedules.FindAsync(model.ScheduleId);
            if (existing == null) return NotFound();

            if (!model.StartTime.HasValue || !model.EndTime.HasValue)
                ModelState.AddModelError("", "Start time and End time are required.");
            else if (model.StartTime >= model.EndTime)
                ModelState.AddModelError("", "End time must be later than Start time.");

            bool conflict = await _db.Schedules.AnyAsync(s =>
                s.ScheduleId != model.ScheduleId &&
                s.CourseId == model.CourseId &&
                s.Class == model.Class &&
                s.Date == model.Date &&
                model.StartTime < s.EndTime &&
                model.EndTime > s.StartTime
            );

            if (conflict)
                ModelState.AddModelError("", "Schedule time conflicts with existing schedule.");

            if (!ModelState.IsValid)
            {
                BuildDropdowns(model.FacultyId, model.CourseId);
                return View(model);
            }

            existing.CourseId = model.CourseId;
            existing.FacultyId = model.FacultyId;
            existing.Class = model.Class;
            existing.Date = model.Date;
            existing.StartTime = model.StartTime;
            existing.EndTime = model.EndTime;
            existing.Room = model.Room;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _db.Schedules.FindAsync(id);
            if (model == null) return NotFound();

            _db.Schedules.Remove(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ================= DROPDOWNS =================
        private void BuildDropdowns(int facultyId = 0, int courseId = 0)
        {
            ViewBag.Faculties = _db.Faculties
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = f.FacultyName,
                    Selected = f.FacultyId == facultyId
                }).ToList();

            ViewBag.Courses = _db.Courses
                .Where(c => facultyId == 0 || c.FacultyId == facultyId)
                .Select(c => new SelectListItem
                {
                    Value = c.CourseId.ToString(),
                    Text = c.CourseName,
                    Selected = c.CourseId == courseId
                }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesByFaculty(int facultyId)
        {
            var courses = await _db.Courses
                .Where(c => c.FacultyId == facultyId)
                .Select(c => new { courseId = c.CourseId, courseName = c.CourseName })
                .ToListAsync();

            return Json(courses);
        }

        [HttpGet]
        public async Task<IActionResult> GetClassesByCourse(int courseId)
        {
            var classes = await _db.Courses
                .Where(c => c.CourseId == courseId && c.Class != null)
                .Select(c => c.Class!)
                .Distinct()
                .ToListAsync();

            return Json(classes);
        }
    }
}
