using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System.Security.Claims;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin,Faculty,Student")]
    public class FacultyController : Controller
    {
        private readonly SimDbContext _db;
        public FacultyController(SimDbContext db) => _db = db;

        // ========================= 1. INDEX =========================
        public IActionResult Index(string? search)
        {
            IQueryable<Faculty> query = _db.Faculties.Include(f => f.User);

            var role = User.FindFirstValue(ClaimTypes.Role);
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (role == "Admin")
            {
                // Admin thấy tất cả, filter search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.FacultyName.ToLower().Contains(search) ||
                        f.Email.ToLower().Contains(search) ||
                        f.Department.ToLower().Contains(search));
                }
            }
            else if (role == "Faculty")
            {
                // Faculty thấy chính mình
                query = query.Where(f => f.UserId == userId);
            }
            else if (role == "Student")
            {
                var studentClass = _db.Students
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Class)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(studentClass))
                {
                    // 1. Lấy courses ra memory trước
                    var courses = _db.Courses
                        .Where(c => c.Class != null)
                        .ToList();

                    // 2. Xử lý Split trong memory
                    var facultyIds = courses
                        .Where(c => c.Class
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(cl => cl.Trim())
                            .Contains(studentClass))
                        .Select(c => c.FacultyId)
                        .Distinct()
                        .ToList();

                    // 3. Query Faculty
                    query = _db.Faculties
                        .Include(f => f.User)
                        .Where(f => facultyIds.Contains(f.FacultyId));
                }
                else
                {
                    query = Enumerable.Empty<Faculty>().AsQueryable();
                }
            }

            // Áp dụng search cuối cùng (trừ Student và Faculty nếu không muốn)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(f =>
                    f.FacultyName.ToLower().Contains(search) ||
                    f.Email.ToLower().Contains(search) ||
                    f.Department.ToLower().Contains(search));
            }

            return View(query.ToList());
        }

        // ========================= 2. ADD =========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(Faculty faculty, string Username, string Password)
        {
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 8)
                ModelState.AddModelError("Username", "Username must be at least 8 characters");
            else if (!char.IsUpper(Username[0]))
                ModelState.AddModelError("Username", "Username must start with uppercase letter");

            if (_db.Users.Any(u => u.Username == Username))
                ModelState.AddModelError("Username", "Username already exists");

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
                ModelState.AddModelError("Password", "Password must be at least 8 characters");

            if (string.IsNullOrWhiteSpace(faculty.Email) || !faculty.Email.Contains("@gmail.com"))
                ModelState.AddModelError("Email", "Email must contain '@gmail.com'");

            if (_db.Faculties.Any(f => f.Email == faculty.Email))
                ModelState.AddModelError("Email", "Email already exists");

            if (!ModelState.IsValid)
            {
                ViewData["Username"] = Username;
                return View(faculty);
            }

            var user = new User
            {
                Username = Username,
                Email = faculty.Email,
                HashPassword = Password,
                Role = "Faculty",
                CreateAt = DateTime.Now
            };
            _db.Users.Add(user);
            _db.SaveChanges();

            faculty.UserId = user.Id;
            faculty.User = user;
            _db.Faculties.Add(faculty);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Faculty added successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========================= 3. EDIT =========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var faculty = _db.Faculties.Include(f => f.User).FirstOrDefault(f => f.FacultyId == id);
            if (faculty == null) return NotFound();
            ViewData["Username"] = faculty.User?.Username;
            return View(faculty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(Faculty model, string Username, string? Password)
        {
            var faculty = _db.Faculties.Include(f => f.User).FirstOrDefault(f => f.FacultyId == model.FacultyId);
            if (faculty == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@gmail.com"))
                ModelState.AddModelError("Email", "Email must contain '@gmail.com'");
            if (_db.Faculties.Any(f => f.Email == model.Email && f.FacultyId != faculty.FacultyId))
                ModelState.AddModelError("Email", "Email already exists");

            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 8)
                ModelState.AddModelError("Username", "Username must be at least 8 characters");
            else if (!char.IsUpper(Username[0]))
                ModelState.AddModelError("Username", "Username must start with uppercase letter");
            if (_db.Users.Any(u => u.Username == Username && u.Id != faculty.UserId))
                ModelState.AddModelError("Username", "Username already exists");

            if (!string.IsNullOrWhiteSpace(Password) && Password.Length < 8)
                ModelState.AddModelError("Password", "Password must be at least 8 characters");

            if (!ModelState.IsValid)
            {
                ViewData["Username"] = Username;
                return View(model);
            }

            faculty.FacultyName = model.FacultyName;
            faculty.Email = model.Email;
            faculty.Department = model.Department;
            faculty.Phone = model.Phone;
            faculty.HireDate = model.HireDate;

            faculty.User.Username = Username;
            if (!string.IsNullOrWhiteSpace(Password))
                faculty.User.HashPassword = Password;

            _db.SaveChanges();
            TempData["SuccessMessage"] = $"Faculty '{faculty.FacultyName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ========================= 4. ASSIGNED COURSES =========================
        [Authorize(Roles = "Admin, Faculty")]
        public async Task<IActionResult> AssignedCourses()
        {
            // Nếu là Admin, trả về tất cả courses
            if (User.IsInRole("Admin"))
            {
                var courses = await _db.Courses
                    .Include(c => c.FacultyCourses)
                    .ToListAsync();

                return View(courses); // View nhận IEnumerable<Course>
            }

            // Nếu là Faculty, lấy UserId
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Tìm Faculty theo UserId, dùng FirstOrDefaultAsync để tránh lỗi
            var faculty = await _db.Faculties
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (faculty == null)
            {
                // Không tìm thấy faculty, trả về view rỗng
                TempData["ErrorMessage"] = "Faculty not found.";
                return View(new List<Course>());
            }

            int facultyId = faculty.FacultyId;

            // Lấy danh sách courses mà faculty được giao
            var facultyCourses = await _db.FacultyCourses
                .Where(fc => fc.FacultyId == facultyId)
                .Include(fc => fc.Course)
                .Select(fc => fc.Course)
                .ToListAsync();

            return View(facultyCourses);
        }



        // ========================= 5. RECORD ATTENDANCE =========================
        [HttpGet]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> RecordAttendance(int scheduleId)
        {
            // Lấy schedule theo Id
            var schedule = await _db.Schedules
                .Include(s => s.Course)
                .Include(s => s.Course.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null) return NotFound();

            // Lấy danh sách sinh viên của course và class tương ứng
            var students = schedule.Course.Enrollments
                .Where(e => e.Student.Class == schedule.Class)
                .Select(e => e.Student)
                .ToList();

            // Lấy attendance hiện tại của các sinh viên trong ngày này
            var existingAttendance = await _db.Attendances
                .Where(a => a.CourseId == schedule.CourseId && a.Date == schedule.Date)
                .ToListAsync();

            // Tạo dictionary StudentId -> Present, tránh trùng key
            var attendanceDict = existingAttendance
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.First().Present);

            // Truyền thông tin sang view
            ViewBag.ScheduleId = schedule.ScheduleId;
            ViewBag.CourseId = schedule.CourseId;
            ViewBag.CourseName = schedule.Course.CourseName;
            ViewBag.ClassName = schedule.Class;
            ViewBag.Date = schedule.Date;
            ViewBag.AttendanceDict = attendanceDict;

            return View(students); // Model là danh sách Student
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Faculty,Admin")]
        public async Task<IActionResult> RecordAttendanceSubmit(int scheduleId, Dictionary<int, bool> attendance)
        {
            var schedule = await _db.Schedules
                .Include(s => s.Course)
                .Include(s => s.Course.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId);

            if (schedule == null) return NotFound();

            var students = schedule.Course.Enrollments
                .Where(e => e.Student.Class == schedule.Class)
                .Select(e => e.Student)
                .ToList();

            foreach (var student in students)
            {
                // Kiểm tra nếu đã có attendance cho student + course + date
                var existing = await _db.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == student.StudentId
                                           && a.CourseId == schedule.CourseId
                                           && a.Date == schedule.Date);

                bool isPresent = attendance.ContainsKey(student.StudentId) && attendance[student.StudentId];

                if (existing != null)
                {
                    // Update trạng thái
                    existing.Present = isPresent;
                }
                else
                {
                    // Tạo mới
                    var newAttendance = new Attendance
                    {
                        StudentId = student.StudentId,
                        CourseId = schedule.CourseId,
                        Date = schedule.Date,
                        Present = isPresent
                    };
                    _db.Attendances.Add(newAttendance);
                }
            }

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Attendance recorded successfully!";
            return RedirectToAction("RecordAttendance", new { scheduleId = scheduleId });
        }


        // ========================= 6. DELETE =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var faculty = _db.Faculties.Include(f => f.User).FirstOrDefault(f => f.FacultyId == id);
            if (faculty == null) return NotFound();

            if (faculty.User != null)
                _db.Users.Remove(faculty.User);

            _db.Faculties.Remove(faculty);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Faculty deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
