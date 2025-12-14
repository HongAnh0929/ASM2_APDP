using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System.Security.Claims;

namespace SIMS.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SimDbContext _db;

        public StudentsController(SimDbContext db)
        {
            _db = db;
        }

        // ====================== STUDENT VIEW OWN INFO ======================
        [Authorize(Roles = "Student")]
        public IActionResult Info()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var student = _db.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.UserId == userId);

            return View(student);
        }

        // ====================== LIST + SEARCH STUDENTS ======================
        [Authorize(Roles = "Admin, Faculty, Student")]
        public async Task<IActionResult> Index(string search)
        {
            var query = _db.Students.Include(s => s.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(s =>
                    s.FullName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search) ||
                    s.Class.ToLower().Contains(search)
                );
            }

            ViewBag.SearchTerm = search;
            return View(await query.ToListAsync());
        }

        // ====================== ADD STUDENT (GET) ======================
        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            ViewBag.Classes = _db.Courses
                .Select(c => c.Class)
                .Distinct()
                .ToList();

            return View();
        }

        // ====================== ADD STUDENT (POST) ======================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Add(string FullName, string Major, string Gender, DateTime Dob, float GPA, string AcademicStanding, string Class,string Username, string Password, string Email)
        {
            // ❗ 1. CHECK EMAIL PHẢI LÀ @gmail.com
            if (string.IsNullOrEmpty(Email) || !Email.EndsWith("@gmail.com"))
            {
                ViewBag.EmailError = "Email must be in format @gmail.com";
                return View(); // ❌ không lưu, ❌ không redirect
            }

            // ❗ 2. CHECK USERNAME TRÙNG
            if (_db.Users.Any(u => u.Username == Username))
            {
                TempData["ErrorMessage"] = "Username already exists!";
                return RedirectToAction("Add");
            }

            // 3️⃣ CREATE USER
            var user = new User
            {
                Username = Username,
                Email = Email,
                HashPassword = Password, // plain text theo yêu cầu
                Role = "Student",
                CreateAt = DateTime.Now
            };

            _db.Users.Add(user);
            _db.SaveChanges(); // để có UserId

            // 4️⃣ CREATE STUDENT
            var student = new Student
            {
                FullName = FullName,
                Major = Major,
                Gender = Gender,
                Dob = Dob,
                GPA = GPA,
                AcademicStanding = AcademicStanding,
                Class = Class,
                Email = Email,
                UserId = user.Id
            };

            _db.Students.Add(student);
            _db.SaveChanges(); // để có StudentId

            // 5️⃣ AUTO ENROLL COURSES THEO CLASS
            var courses = _db.Courses
                .Where(c => c.Class == student.Class)
                .ToList();

            foreach (var course in courses)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    StudentId = student.StudentId,
                    CourseId = course.CourseId
                });
            }

            _db.SaveChanges();

            TempData["SuccessMessage"] = "Student created successfully!";
            return RedirectToAction("Index");
        }

        // ====================== EDIT STUDENT (GET) ======================
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var student = _db.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null) return NotFound();

            ViewBag.Classes = _db.Courses
                .Select(c => c.Class)
                .Distinct()
                .ToList();

            return View(student);
        }

        // ====================== EDIT STUDENT (POST) ======================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int StudentId, string FullName, string Major, string Gender, DateTime Dob, float GPA, string AcademicStanding, string Class, string Username, string Password, string Email)
        {
            var student = _db.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == StudentId);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found!";
                return RedirectToAction("Index");
            }

            // Update student info
            student.FullName = FullName ;
            student.Email = Email;
            student.Gender = Gender;
            student.Major = Major;
            student.Dob = Dob;
            student.GPA = GPA;
            student.AcademicStanding = AcademicStanding;
            student.Class = Class;

            // Nếu admin nhập username mới => sửa hoặc tạo account
            if (!string.IsNullOrEmpty(Username))
            {
                User user;
                if (student.User != null)
                {
                    // Đã có user => cập nhật thông tin
                    user = student.User;
                    user.Username = Username;

                    if (!string.IsNullOrEmpty(Password))
                        user.HashPassword = Password;
                }
                else
                {
                    // Tạo user mới nếu faculty chưa có account
                    user = new User
                    {
                        Username = Username,
                        HashPassword = string.IsNullOrEmpty(Password) ? "" : Password,
                        Role = "Student",
                        CreateAt = DateTime.Now
                    };
                    _db.Users.Add(user);
                    _db.SaveChanges(); // Lưu để có Id
                }

                student.UserId = user.Id;
            }

            // Update user email (đồng bộ)
            if (student.User != null)
            {
                student.User.Email = student.Email;
            }

            // Remove old enrollments
            var oldEnrollments = _db.Enrollments
                .Where(e => e.StudentId == student.StudentId);

            _db.Enrollments.RemoveRange(oldEnrollments);
            _db.SaveChanges();

            // Add new enrollments theo Class mới
            var newCourses = _db.Courses
                .Where(c => c.Class == student.Class)
                .ToList();

            foreach (var course in newCourses)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    StudentId = student.StudentId,
                    CourseId = course.CourseId
                });
            }

            _db.SaveChanges();

            TempData["SuccessMessage"] = "Student updated successfully!";
            return RedirectToAction("Index");
        }

        // ====================== DELETE STUDENT ======================
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            // 1. Lấy student kèm user + enrollments
            var student = _db.Students
                .Include(s => s.Enrollments)
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == id);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Student not found!";
                return RedirectToAction("Index");
            }

            // 2. Xóa enrollment
            if (student.Enrollments.Any())
            {
                _db.Enrollments.RemoveRange(student.Enrollments);
            }

            // 3. Xóa student
            _db.Students.Remove(student);

            // 4. Xóa user
            if (student.User != null)
            {
                _db.Users.Remove(student.User);
            }

            _db.SaveChanges();

            TempData["SuccessMessage"] = "Student and user account deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}