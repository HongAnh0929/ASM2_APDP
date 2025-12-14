using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using SIMS.Services;
using System.Security.Claims;

namespace SIMS.Controllers
{
    public class FacultyController : Controller
    {
        private readonly FacultyService _facultyService; // Service xử lý business logic cho Faculty
        private readonly SimDbContext _db; // DbContext để thao tác với database

        public FacultyController(FacultyService facultyService, SimDbContext db)
        {
            _facultyService = facultyService;
            _db = db;
        }

        // ======================= INDEX =======================
        [Authorize(Roles = "Admin, Faculty, Student")] // Chỉ các role này được xem danh sách
        public IActionResult Index(string? search)
        {
            // Lấy danh sách Faculty kèm User liên kết
            var faculties = _db.Faculties
                               .Include(f => f.User) // Join với bảng Users
                               .AsQueryable();

            // Nếu có text search
            if (!string.IsNullOrEmpty(search))
            {
                var term = search.ToLower();
                faculties = faculties.Where(f =>
                    f.FacultyName.ToLower().Contains(term) ||
                    f.Email.ToLower().Contains(term) ||
                    f.Department.ToLower().Contains(term) ||
                    // Tìm theo Username của User (kiểm tra null trước)
                    (f.User != null && f.User.Username.ToLower().Contains(term))
                );
            }

            ViewBag.SearchTerm = search;

            return View(faculties.ToList()); // Trả dữ liệu sang View
        }

        // ======================= ADD (GET) =======================
        [Authorize(Roles = "Admin")] // Chỉ admin được thêm Faculty
        [HttpGet]
        public IActionResult Add()
        {
            // Lấy danh sách Users có role Faculty để gán vào Faculty mới
            ViewBag.Users = new SelectList(_db.Users.Where(u => u.Role == "Faculty"), "Id", "Username");
            return View();
        }

        // ======================= ADD (POST) =======================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(
    [Bind("FacultyName,Email,Department,Phone,HireDate,UserId")] Faculty faculty,
    string Username,
    string Password)
        {
            // ================= EMAIL VALIDATION (@gmail.com) =================
            if (string.IsNullOrEmpty(faculty.Email) || !faculty.Email.EndsWith("@gmail.com"))
            {
                ViewBag.EmailError = "Email must be in format @gmail.com";
                return View(faculty); // ❌ không lưu, ❌ không redirect
            }

            // Validate cơ bản
            if (string.IsNullOrEmpty(faculty.FacultyName))
            {
                TempData["ErrorMessage"] = "Faculty name is required.";
                return View(faculty);
            }

            // ================= USER LOGIC =================
            if (!string.IsNullOrEmpty(Username))
            {
                var existingUser = _db.Users.FirstOrDefault(u => u.Username == Username);

                if (existingUser != null)
                {
                    faculty.UserId = existingUser.Id;
                }
                else
                {
                    if (string.IsNullOrEmpty(Password))
                    {
                        TempData["ErrorMessage"] = "Password is required for new user!";
                        return View(faculty);
                    }

                    var newUser = new User
                    {
                        Username = Username,
                        Email = faculty.Email,
                        Role = "Faculty",
                        CreateAt = DateTime.Now,
                        HashPassword = Password // theo yêu cầu của bạn
                    };

                    _db.Users.Add(newUser);
                    _db.SaveChanges();

                    faculty.UserId = newUser.Id;
                }
            }

            // ================= SAVE FACULTY =================
            _db.Faculties.Add(faculty);
            _db.SaveChanges();

            TempData["SuccessMessage"] = $"Faculty '{faculty.FacultyName}' added successfully!";
            return RedirectToAction("Index");
        }

        // ======================= EDIT (GET) =======================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var faculty = _db.Faculties.Include(f => f.User).FirstOrDefault(f => f.FacultyId == id);
            if (faculty == null) return NotFound();

            // Gửi danh sách Users để admin có thể chọn gán
            ViewBag.Users = new SelectList(_db.Users.Where(u => u.Role == "Faculty"), "Id", "Username", faculty.UserId);

            return View(faculty);
        }

        // ======================= EDIT (POST) =======================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int facultyId, string FacultyName, string Email, string Department, string Phone, DateTime? HireDate,
                                  string NewUserName, string NewUserPassword, int? UserId)
        {
            var faculty = _db.Faculties.Include(f => f.User).FirstOrDefault(f => f.FacultyId == facultyId);
            if (faculty == null) return NotFound();

            // Cập nhật thông tin cơ bản
            faculty.FacultyName = FacultyName;
            faculty.Email = Email;
            faculty.Department = Department;
            faculty.Phone = Phone;
            faculty.HireDate = HireDate;

            // Nếu admin nhập username mới => sửa hoặc tạo account
            if (!string.IsNullOrEmpty(NewUserName))
            {
                User user;
                if (faculty.User != null)
                {
                    // Đã có user => cập nhật thông tin
                    user = faculty.User;
                    user.Username = NewUserName;

                    if (!string.IsNullOrEmpty(NewUserPassword))
                        user.HashPassword = NewUserPassword; 
                }
                else
                {
                    // Tạo user mới nếu faculty chưa có account
                    user = new User
                    {
                        Username = NewUserName,
                        HashPassword = string.IsNullOrEmpty(NewUserPassword) ? "" : NewUserPassword,
                        Role = "Faculty",
                        CreateAt = DateTime.Now
                    };
                    _db.Users.Add(user);
                    _db.SaveChanges(); // Lưu để có Id
                }

                faculty.UserId = user.Id;
            }
            else if (UserId.HasValue)
            {
                // Nếu admin chọn user có sẵn từ dropdown
                faculty.UserId = UserId.Value;
            }

            _db.SaveChanges();

            TempData["SuccessMessage"] = $"Faculty '{faculty.FacultyName}' updated successfully!";
            return RedirectToAction("Index");
        }

        // ======================= DELETE =======================
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var faculty = _db.Faculties
                .Include(f => f.User)
                .Include(f => f.Courses)
                .FirstOrDefault(f => f.FacultyId == id);

            if (faculty == null) return NotFound();

            _db.Courses.RemoveRange(faculty.Courses);
            _db.Faculties.Remove(faculty);
            _db.Users.Remove(faculty.User);

            _db.SaveChanges();

            TempData["SuccessMessage"] = $"Faculty '{faculty.FacultyName}' deleted successfully!";
            return RedirectToAction("Index");
        }

        // ======================= ASSIGNED COURSES =======================
        [Authorize(Roles = "Admin, Faculty")]
        public async Task<IActionResult> AssignedCourses()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return RedirectToAction("Index", "Login");

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToAction("Index", "Login");

            // Nếu Admin => xem toàn bộ
            if (User.IsInRole("Admin"))
            {
                var allCourses = await _db.Courses.ToListAsync();
                ViewBag.FacultyId = 0;
                return View(allCourses);
            }

            // Nếu Faculty => chỉ xem các course mình dạy
            var faculty = await _db.Faculties.FirstOrDefaultAsync(f => f.UserId == userId);
            if (faculty == null) return RedirectToAction("Index", "Login");

            var courses = await _db.Courses
                .Where(c => c.FacultyId == faculty.FacultyId)
                .ToListAsync();

            ViewBag.FacultyId = faculty.FacultyId;
            return View(courses);
        }

        // ======================= RECORD ATTENDANCE (GET) =======================
        [Authorize(Roles = "Admin, Faculty")]
        [HttpGet]
        public async Task<IActionResult> RecordAttendance(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            Course course;

            if (User.IsInRole("Faculty"))
            {
                var faculty = await _db.Faculties.FirstOrDefaultAsync(f => f.UserId == userId);
                if (faculty == null) return RedirectToAction("AssignedCourses");

                course = await _db.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.FacultyId == faculty.FacultyId);

                if (course == null) return RedirectToAction("AssignedCourses");
            }
            else
            {
                course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
                if (course == null) return RedirectToAction("Index", "Courses");
            }

            var students = await _db.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.Student)
                .Select(e => e.Student)
                .ToListAsync();

            var today = DateTime.Today;

            var attendanceDict = await _db.Attendances
                .Where(a => a.CourseId == courseId && a.Date == today)
                .ToDictionaryAsync(a => a.StudentId, a => a.Present);

            ViewBag.CourseId = courseId;
            ViewBag.CourseName = course.CourseName;
            ViewBag.ClassName = course.Class;
            ViewBag.Date = today;
            ViewBag.AttendanceDict = attendanceDict;

            return View(students);
        }

        // ======================= RECORD ATTENDANCE (POST) =======================
        [Authorize(Roles = "Admin, Faculty")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordAttendanceSubmit(int courseId, DateTime date, List<int>? presentStudentIds)
        {
            var students = await _db.Enrollments
                .Where(e => e.CourseId == courseId)
                .Select(e => e.Student)
                .ToListAsync();

            foreach (var student in students)
            {
                bool present = presentStudentIds != null &&
                               presentStudentIds.Contains(student.StudentId);

                var existing = await _db.Attendances.FirstOrDefaultAsync(a =>
                    a.CourseId == courseId &&
                    a.StudentId == student.StudentId &&
                    a.Date == date
                );

                if (existing != null)
                {
                    existing.Present = present;
                }
                else
                {
                    _db.Attendances.Add(new Attendance
                    {
                        CourseId = courseId,
                        StudentId = student.StudentId,
                        Date = date,
                        Present = present
                    });
                }
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Attendance saved successfully!";

            return RedirectToAction("RecordAttendance", new { courseId });
        }


        // ======================= MANAGE ACADEMIC RECORD =======================
        [Authorize(Roles = "Admin, Faculty")]
        public async Task<IActionResult> ManageAcademicRecord()
        {
            var students = await _facultyService.GetAllStudents();
            return View(students);
        }
    }
}