using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System.Security.Claims;

namespace SIMS.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly SimDbContext _db;

        public StudentsController(SimDbContext db)
        {
            _db = db;
        }

        // ================= STUDENT VIEW OWN INFO =================
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Info()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var student = await _db.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null) return NotFound();
            return View(student);
        }

        // ================= LIST =================
        [Authorize(Roles = "Admin,Faculty,Student")]
        public async Task<IActionResult> Index(string? search)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            IQueryable<Student> students;

            if (role == "Admin")
            {
                students = _db.Students.Include(s => s.User);
            }
            else if (role == "Faculty")
            {
                var facultyId = await _db.Faculties
                    .Where(f => f.UserId == userId)
                    .Select(f => f.FacultyId)
                    .FirstOrDefaultAsync();

                students = _db.Enrollments
                    .Where(e => e.Course.FacultyCourses.Any(fc => fc.FacultyId == facultyId))
                    .Select(e => e.Student)
                    .Distinct();
            }
            else // Student
            {
                var student = await _db.Students.FirstAsync(s => s.UserId == userId);
                students = _db.Students.Where(s => s.Class == student.Class);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                students = students.Where(s =>
                    s.FullName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search));
            }

            return View(await students.AsNoTracking().ToListAsync());
        }

        // ================= ADD =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.ClassesList = new SelectList(
                _db.Courses.Select(c => c.Class).Distinct()
            );
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Student student, string Username, string Password)
        {
            // ---------- VALIDATION ----------
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 8)
                ModelState.AddModelError("Username", "Username must be at least 8 characters");
            else if (!char.IsUpper(Username[0]))
                ModelState.AddModelError("Username", "Username must start with an uppercase letter");

            if (await _db.Users.AnyAsync(u => u.Username == Username))
                ModelState.AddModelError("Username", "Username already exists");

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
                ModelState.AddModelError("Password", "Password must be at least 8 characters");

            // Email validation
            if (string.IsNullOrWhiteSpace(student.Email) || !student.Email.Contains("@gmail.com"))
                ModelState.AddModelError("Email", "Email must contain '@gmail.com'");

            if (await _db.Students.AnyAsync(s => s.Email == student.Email))
                ModelState.AddModelError("Email", "Email already exists");

            if (!ModelState.IsValid)
            {
                ViewBag.ClassesList = new SelectList(
                    _db.Courses.Select(c => c.Class).Distinct()
                );
                return View(student);
            }

            // ---------- CREATE USER (lưu password trực tiếp) ----------
            var user = new User
            {
                Username = Username,
                Email = student.Email,
                HashPassword = Password, // <-- không hash nữa
                Role = "Student",
                CreateAt = DateTime.Now
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // ---------- CREATE STUDENT ----------
            student.UserId = user.Id;
            _db.Students.Add(student);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= EDIT =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _db.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();

            ViewBag.ClassesList = new SelectList(
                _db.Courses.Select(c => c.Class).Distinct(),
                student.Class
            );

            return View(student);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Student model, string Username, string? Password)
        {
            var student = await _db.Students.Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == model.StudentId);

            if (student == null) return NotFound();

            // ---------- VALIDATION ----------
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 8)
                ModelState.AddModelError("Username", "Username must be at least 8 characters");
            else if (!char.IsUpper(Username[0]))
                ModelState.AddModelError("Username", "Username must start with an uppercase letter");

            if (await _db.Users.AnyAsync(u => u.Username == Username && u.Id != student.UserId))
                ModelState.AddModelError("Username", "Username already exists");

            if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 8)
                ModelState.AddModelError("Password", "Password must be at least 8 characters");

            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@gmail.com"))
                ModelState.AddModelError("Email", "Email must contain '@gmail.com'");

            if (await _db.Students.AnyAsync(s => s.Email == model.Email && s.StudentId != student.StudentId))
                ModelState.AddModelError("Email", "Email already exists");

            if (!ModelState.IsValid)
            {
                ViewBag.ClassesList = new SelectList(
                    _db.Courses.Select(c => c.Class).Distinct(),
                    model.Class
                );
                return View(model);
            }

            // ---------- UPDATE STUDENT ----------
            student.FullName = model.FullName;
            student.Email = model.Email;
            student.Major = model.Major;
            student.Gender = model.Gender;
            student.Dob = model.Dob;
            student.GPA = model.GPA;
            student.AcademicStanding = model.AcademicStanding;
            student.Class = model.Class;

            // ---------- UPDATE USER ----------
            student.User.Username = Username;
            if (!string.IsNullOrWhiteSpace(Password))
                student.User.HashPassword = Password; // <-- không hash nữa

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Student updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= DELETE =================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _db.Students
                .Include(s => s.User)
                .Include(s => s.Enrollments)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();

            if (student.Enrollments.Any())
                _db.Enrollments.RemoveRange(student.Enrollments);

            _db.Students.Remove(student);

            if (student.User != null)
                _db.Users.Remove(student.User);

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ================= STUDENT ATTENDANCE VIEW =================
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Attendance()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return Unauthorized();

            // Lấy tất cả attendance + include Course + Faculty
            var attendances = await _db.Attendances
                .Where(a => a.StudentId == student.StudentId)
                .Include(a => a.Course)
                    .ThenInclude(c => c.Faculty)
                .ToListAsync();

            // Chỉ lấy duy nhất 1 bản ghi/ngày
            var uniqueAttendances = attendances
                .GroupBy(a => a.Date)
                .Select(g => g.OrderByDescending(a => a.AttendanceId).First())
                .OrderByDescending(a => a.Date)
                .ToList();

            ViewBag.StudentName = student.FullName;
            return View(uniqueAttendances);
        }

        // ========================= COURSE STUDENTS =========================
        public IActionResult CourseStudents(int courseId, string className)
        {
            // Lấy thông tin course
            var course = _db.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (course == null) return NotFound();

            // Lọc sinh viên theo course + class chính xác
            var students = _db.Enrollments
                .Where(e => e.CourseId == courseId && e.Student.Class == className)
                .Include(e => e.Student)
                .Select(e => e.Student)
                .ToList();

            ViewBag.Course = course;
            ViewBag.ClassName = className;
            return View(students);
        }


    }
}
