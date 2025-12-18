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
    public class CoursesController : Controller
    {
        private readonly SimDbContext _db;

        public CoursesController(SimDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Lấy tất cả courses với Faculty
            var coursesQuery = _db.Courses.Include(c => c.Faculty).AsQueryable();

            if (role == "Faculty")
            {
                var facultyId = _db.Faculties
                    .Where(f => f.UserId == userId)
                    .Select(f => f.FacultyId)
                    .FirstOrDefault();

                coursesQuery = coursesQuery.Where(c => c.FacultyId == facultyId);
            }
            else if (role == "Student")
            {
                var studentClass = _db.Students
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Class)
                    .FirstOrDefault()?.Trim();

                if (!string.IsNullOrEmpty(studentClass))
                {
                    var coursesList = coursesQuery
                        .Where(c => c.Class != null)
                        .ToList()
                        .Where(c => c.Class.Split(',')
                                            .Select(cl => cl.Trim())
                                            .Contains(studentClass))
                        .ToList();

                    coursesQuery = coursesList.AsQueryable();
                }
                else
                {
                    coursesQuery = Enumerable.Empty<Course>().AsQueryable();
                }
            }

            // ===== Load danh sách sinh viên theo lớp (Admin) để Enroll tự động =====
            if (User.IsInRole("Admin"))
            {
                // Lấy toàn bộ sinh viên, nhóm theo class
                var studentsByClass = _db.Students
                    .GroupBy(s => s.Class)
                    .ToDictionary(g => g.Key, g => g.ToList());

                ViewBag.StudentsByClass = studentsByClass;
            }

            return View(coursesQuery.AsNoTracking().ToList());
        }

        // =========================
        // ADD COURSE - GET
        // =========================
        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            LoadClassesDropdown();
            LoadFacultiesDropdown();
            return View();
        }

        // =========================
        // ADD COURSE - POST
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Course model, string classInput)
        {
            if (model.Class == "Other")
            {
                model.Class = classInput?.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Class))
                ModelState.AddModelError("Class", "Class is required.");

            if (model.FacultyId == 0)
                ModelState.AddModelError("FacultyId", "Faculty is required.");

            if (model.StartDate.HasValue && model.EndDate.HasValue &&
                model.StartDate > model.EndDate)
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");

            if (!ModelState.IsValid)
            {
                LoadClassesDropdown(model.Class);
                LoadFacultiesDropdown(model.FacultyId);
                return View(model);
            }

            _db.Courses.Add(model);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Course added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT COURSE - GET
        // =========================
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var course = _db.Courses.Find(id);
            if (course == null) return NotFound();

            LoadClassesDropdown(course.Class);
            LoadFacultiesDropdown(course.FacultyId);

            return View(course);
        }

        // =========================
        // EDIT COURSE - POST
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Course model, string classInput)
        {
            if (model.Class == "Other")
            {
                model.Class = classInput?.Trim();
            }

            if (string.IsNullOrWhiteSpace(model.Class))
                ModelState.AddModelError("Class", "Class is required.");

            if (model.FacultyId == 0)
                ModelState.AddModelError("FacultyId", "Faculty is required.");

            if (model.StartDate.HasValue && model.EndDate.HasValue &&
                model.StartDate > model.EndDate)
                ModelState.AddModelError("EndDate", "End Date must be after Start Date.");

            if (!ModelState.IsValid)
            {
                LoadClassesDropdown(model.Class);
                LoadFacultiesDropdown(model.FacultyId);
                return View(model);
            }

            var course = _db.Courses.Find(model.CourseId);
            if (course == null) return NotFound();

            course.CourseName = model.CourseName;
            course.Credits = model.Credits;
            course.Class = model.Class;
            course.FacultyId = model.FacultyId;
            course.StartDate = model.StartDate;
            course.EndDate = model.EndDate;

            _db.SaveChanges();

            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE COURSE
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var course = _db.Courses
                .Include(c => c.Enrollments)
                .FirstOrDefault(c => c.CourseId == id);

            if (course == null)
            {
                TempData["ErrorMessage"] = "Course not found.";
                return RedirectToAction(nameof(Index));
            }

            if (course.Enrollments.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete course with enrolled students.";
                return RedirectToAction(nameof(Index));
            }

            _db.Courses.Remove(course);
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Course deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
        // =========================
        private void LoadClassesDropdown(string? selected = null)
        {
            var classes = _db.Courses
                .Select(c => c.Class)
                .Where(c => c != null)
                .Distinct()
                .ToList();

            var selectList = classes.Select(c => new SelectListItem
            {
                Text = c,
                Value = c,
                Selected = c == selected
            }).ToList();

            // Thêm option "Other" để hiển thị input
            selectList.Add(new SelectListItem
            {
                Text = "Other",
                Value = "Other",
                Selected = selected == "Other"
            });

            ViewBag.ClassesList = selectList;
        }

        private void LoadFacultiesDropdown(int selectedId = 0)
        {
            var faculties = _db.Faculties
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = f.FacultyName,
                    Selected = f.FacultyId == selectedId
                })
                .ToList();

            faculties.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "-- Select Faculty --",
                Selected = selectedId == 0
            });

            ViewBag.Faculties = faculties;
        }


        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Attendance()
        {
            // Lấy user hiện tại
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Lấy student tương ứng
            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return Unauthorized();

            // Lấy danh sách điểm danh của student này
            var attendances = await _db.Attendances
                .Where(a => a.StudentId == student.StudentId)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Faculty)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            ViewBag.StudentName = student.FullName;

            // Trả về View với model là IEnumerable<Attendance>
            return View(attendances);
        }

        public IActionResult CourseStudents(int courseId)
        {
            var course = _db.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (course == null) return NotFound();

            var courseClasses = course.Class?.Split(',')
                                   .Select(c => c.Trim())
                                   .ToList() ?? new List<string>();

            // Lấy sinh viên đăng ký trong course và thuộc lớp course
            var students = _db.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.Student)
                .Select(e => e.Student)
                .Where(s => courseClasses.Contains(s.Class.Trim()))
                .ToList();

            ViewBag.Course = course;
            return View(students);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnrollAllStudents(int courseId, string className)
        {
            // Lấy tất cả sinh viên lớp đó
            var students = _db.Students.Where(s => s.Class == className).ToList();

            foreach (var student in students)
            {
                // Kiểm tra đã enroll chưa
                var exists = _db.Enrollments.Any(e => e.CourseId == courseId && e.StudentId == student.StudentId);
                if (!exists)
                {
                    _db.Enrollments.Add(new Enrollment
                    {
                        CourseId = courseId,
                        StudentId = student.StudentId,
                    });
                }
            }

            _db.SaveChanges();
            TempData["SuccessMessage"] = $"All students in class {className} enrolled successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
