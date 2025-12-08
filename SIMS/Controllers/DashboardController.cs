using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using System.Security.Claims;

namespace SIMS.Controllers
{
    // [Authorize] yêu cầu người dùng phải đăng nhập thì mới truy cập được vào controller này.
    // Nếu chưa login → redirect về trang login (middleware của ASP.NET tự xử lý).
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly SimDbContext _db;

        public DashboardController(SimDbContext db)
        {
            // Nhận DbContext qua Dependency Injection
            // Cho phép controller truy cập database: Students, Courses, Faculties...
            _db = db;
        }

        public IActionResult Index()
        {
            // Khi người dùng login thành công, ASP.NET tạo 1 ClaimsPrincipal lưu thông tin user.
            // ClaimTypes.Role chứa quyền của người dùng: "Admin", "Faculty", "Student".
            var role = User.FindFirstValue(ClaimTypes.Role);

            // Dựa vào role, hệ thống tự redirect đến dashboard tương ứng.
            // Đây chính là điều hướng trung tâm khi user vừa login xong.
            return role switch
            {
                "Admin" => RedirectToAction("Admin"),     // Nếu là Admin → load trang Admin Dashboard
                "Faculty" => RedirectToAction("Faculty"), // Nếu là giảng viên → load Faculty Dashboard
                "Student" => RedirectToAction("Student"), // Nếu là sinh viên → load Student Dashboard
                _ => RedirectToAction("Index", "Login")   // Nếu role không hợp lệ → quay về Login
            };
        }

        // ==========================
        // ADMIN DASHBOARD
        // ==========================
        [Authorize(Roles = "Admin")]
        // Chỉ Admin mới được vào action này, các role khác sẽ bị chặn
        public IActionResult Admin()
        {
            // Lấy số lượng dữ liệu để hiển thị lên các thẻ thống kê (cards)
            ViewBag.TotalStudents = _db.Students.Count();
            ViewBag.TotalFaculty = _db.Faculties.Count();
            ViewBag.TotalCourses = _db.Courses.Count();

            // ---------- BAR CHART ----------
            // Lấy tên các khoa để làm label cho biểu đồ cột
            ViewBag.FacultyNames = _db.Faculties
                .Select(f => f.FacultyName)
                .ToList();

            // Đếm số lượng khóa học (course) theo từng khoa
            // Mỗi phần tử trong danh sách → là tổng số courses thuộc về 1 khoa
            ViewBag.CourseCounts = _db.Faculties
                .Select(f => _db.Courses.Count(c => c.FacultyId == f.FacultyId))
                .ToList();

            // ---------- DOUGHNUT CHART ----------
            // Lấy danh sách tên các lớp (VD: IT01, IT02...)
            ViewBag.ClassNames = _db.Students
                .Select(s => s.Class)
                .Distinct()     // chỉ lấy mỗi class 1 lần
                .ToList();

            // Đếm số sinh viên trong từng lớp để vẽ biểu đồ doughnut
            ViewBag.StudentCounts = _db.Students
                .GroupBy(s => s.Class)   // gom các student theo class
                .Select(g => g.Count())  // đếm số lượng mỗi nhóm
                .ToList();

            // Trả về view Admin.cshtml
            return View();
        }

        // ==========================
        // STUDENT DASHBOARD
        // ==========================
        [Authorize(Roles = "Student")]
        public IActionResult Student()
        {
            // Lấy userId từ Claim khi user login
            // Claim này được gán khi người dùng đăng nhập thành công
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Tìm student liên kết với user đó (1 user = 1 student)
            var student = _db.Students
                .Include(s => s.User) // lấy thêm thông tin User
                .FirstOrDefault(s => s.UserId == userId);

            // Nếu không tìm thấy student → có thể user login sai kiểu
            if (student == null) return RedirectToAction("Login", "Account");

            // Lấy tất cả courses sinh viên đã đăng ký (Enrollment table)
            var courses = _db.Enrollments
                .Where(e => e.StudentId == student.StudentId) // lọc theo StudentId
                .Include(e => e.Course)                       // lấy thông tin Course
                    .ThenInclude(c => c.Faculty)              // lấy thêm Faculty của Course
                .Select(e => e.Course)                        // chỉ lấy Course
                .ToList();

            // Gửi danh sách courses ra View thông qua ViewBag
            ViewBag.Courses = courses;

            // Gửi model student ra View để hiển thị thông tin cá nhân
            return View(student);
        }

        // ==========================
        // FACULTY DASHBOARD
        // ==========================
        [Authorize(Roles = "Faculty")]
        public IActionResult Faculty()
        {
            // 1. Lấy UserId từ claim (do LoginController đã lưu vào cookie sau khi đăng nhập)
            // ClaimTypes.NameIdentifier: đây chính là UserId của tài khoản đăng nhập
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Nếu không tìm thấy UserId → user chưa login → chuyển về trang Login
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            // Chuyển UserId từ chuỗi sang số nguyên
            int userId = int.Parse(userIdClaim);

            // 2. Truy vấn bảng Faculties để tìm giảng viên có UserId = người đang đăng nhập
            // Vì mỗi tài khoản Faculty được map 1-1 với User
            var faculty = _db.Faculties.FirstOrDefault(f => f.UserId == userId);

            // Nếu tài khoản login không phải Faculty → báo lỗi
            if (faculty == null)
                return Content("Faculty not found for this account.");

            // Lấy FacultyId để truy vấn khóa học & lịch dạy
            int facultyId = faculty.FacultyId;

            // 3. Lấy danh sách course mà giảng viên này được phân công dạy
            // Dựa vào c.FacultyId đã được gán từ trước (bảng Courses có cột FacultyId)
            var assignedCourses = _db.Courses
                .Where(c => c.FacultyId == facultyId)
                .ToList();

            // 4. Lấy lịch dạy của giảng viên trong NGÀY HÔM NAY
            var today = DateTime.Today;

            var todaySchedule = _db.Schedules
                .Include(s => s.Course)       // Include để load CourseName từ Course (Navigation Property)
                .Where(s => s.FacultyId == facultyId && s.Date == today)
                .ToList();

            // 5. Gửi lịch dạy của ngày hôm nay sang View (ViewBag dùng cho dữ liệu phụ)
            ViewBag.TodaySchedule = todaySchedule;

            // Trả Assigned Courses làm Model chính của View (View sẽ nhận List<Course>)
            return View(assignedCourses);
        }
    }
}
