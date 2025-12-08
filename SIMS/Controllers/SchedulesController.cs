using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin, Faculty, Student")] // Faculty chỉ xem Index
    public class SchedulesController : Controller
    {
        private readonly SimDbContext _db;

        public SchedulesController(SimDbContext db)
        {
            _db = db;
        }

        // ===========================
        //  LIST SCHEDULES
        // ===========================
        public async Task<IActionResult> Index(int? courseId)
        {
            var query = _db.Schedules
                           .Include(s => s.Faculty)
                           .Include(s => s.Course)
                           .AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(s => s.CourseId == courseId.Value);
            }

            var list = await query
                           .OrderBy(s => s.Date)
                           .ThenBy(s => s.Time)
                           .ToListAsync();

            return View(list);
        }

        // ===========================
        //  ADD SCHEDULE
        // ===========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add()
        {
            BuildDropdowns();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Schedule model)
        {
            if (!ModelState.IsValid)
            {
                BuildDropdowns(model.FacultyId, model.CourseId);
                return View(model);
            }

            _db.Schedules.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule added.";
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        //  EDIT SCHEDULE
        // ===========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _db.Schedules
                                 .Include(s => s.Course)
                                 .Include(s => s.Faculty)
                                 .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (model == null) return NotFound();

            BuildDropdowns(model.FacultyId, model.CourseId);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Schedule model)
        {
            if (!ModelState.IsValid)
            {
                BuildDropdowns(model.FacultyId, model.CourseId);
                return View(model);
            }

            var existing = await _db.Schedules.FindAsync(model.ScheduleId);
            if (existing == null) return NotFound();

            existing.FacultyId = model.FacultyId;
            existing.Date = model.Date;
            existing.Time = model.Time;
            existing.Room = model.Room;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Schedule updated.";
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        //  DELETE SCHEDULE
        // ===========================
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

        // ===========================
        //  BUILD DROPDOWNS
        // ===========================
        private void BuildDropdowns(int selectedFacultyId = 0, int selectedCourseId = 0)
        {
            ViewBag.Faculties = _db.Faculties
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = f.FacultyName,
                    Selected = f.FacultyId == selectedFacultyId
                })
                .ToList();

            ViewBag.Courses = _db.Courses
                .Select(c => new SelectListItem
                {
                    Value = c.CourseId.ToString(),
                    Text = c.CourseName,
                    Selected = c.CourseId == selectedCourseId
                })
                .ToList();
        }
    }
}
