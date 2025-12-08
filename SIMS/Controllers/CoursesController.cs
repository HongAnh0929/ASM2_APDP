using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;

namespace SIMS.Controllers
{
    public class CoursesController : Controller
    {
        private readonly SimDbContext _db;
        // SimDbContext là lớp kết nối tới database
        // _db được sử dụng để truy vấn bảng Courses, Faculties,...

        public CoursesController(SimDbContext db)
        {
            // Dependency Injection tự động truyền DbContext vào controller
            _db = db;
        }

        // -------------------------------
        // 1. LẤY DANH SÁCH KHÓA HỌC (LIST)
        // -------------------------------
        [Authorize(Roles = "Admin, Faculty, Student")]
        [HttpGet]
        public IActionResult Index()
        {
            // Lấy tất cả khóa học từ bảng Courses
            // Include(Faculty) để lấy thêm thông tin khoa (bảng Faculties)
            var courses = _db.Courses.Include(c => c.Faculty).ToList();

            // Trả dữ liệu qua View → View sẽ hiển thị danh sách khóa học
            return View(courses);
        }

        // -------------------------------
        // 2. TÌM KIẾM KHÓA HỌC
        // -------------------------------
        [Authorize(Roles = "Admin, Faculty, Student")]
        public IActionResult Index(string search)
        {
            // Bắt đầu bằng IQueryable để thêm điều kiện linh hoạt
            var courses = _db.Courses
                .Include(c => c.Faculty)
                .AsQueryable();

            // Nếu có từ khóa search
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();

                // Lọc dữ liệu theo CourseName, Class, FacultyName
                courses = courses.Where(c =>
                    c.CourseName.ToLower().Contains(search) ||
                    c.Class.ToLower().Contains(search) ||
                    c.Faculty.FacultyName.ToLower().Contains(search)
                );
            }

            // Trả về danh sách đã lọc
            return View(courses.ToList());
        }

        // -------------------------------
        // 3. FORM ADD COURSE (GET)
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add(Course course)
        {
            // ⚠️ Lưu ý: Logic này không dùng đúng chuẩn
            // GET không bao giờ nên thêm vào DB.
            // Nhưng ở đây vẫn để nguyên để giải thích:

            // ModelState.IsValid → kiểm tra model có hợp lệ không
            if (ModelState.IsValid)
            {
                // Thêm khóa học vào DB
                _db.Courses.Add(course);
                _db.SaveChanges();

                // Trở về danh sách
                return RedirectToAction("Index");
            }

            // Nếu dữ liệu chưa hợp lệ → hiển thị lại form Add
            return View(course);
        }

        // -------------------------------
        // 4. XỬ LÝ ADD COURSE (POST)
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int FacultyId, string CourseName, string Class, int Credits, DateTime? StartDate, DateTime? EndDate)
        {
            // Kiểm tra các field bắt buộc
            if (FacultyId == 0 || string.IsNullOrEmpty(CourseName))
            {
                TempData["ErrorMessage"] = "Please select a faculty and enter course name.";

                // Load lại dropdown cho view
                BuildFacultyDropdown();
                return View();
            }

            // Tạo đối tượng Course mới
            var course = new Course
            {
                CourseName = CourseName,
                Credits = Credits,
                Class = Class,
                FacultyId = FacultyId,
                StartDate = StartDate,
                EndDate = EndDate
            };

            // Lưu vào DB
            _db.Courses.Add(course);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Course added successfully!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // 5. LẤY DỮ LIỆU ĐỂ LOAD FORM EDIT
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            // Lấy dữ liệu khóa học theo ID
            var course = _db.Courses.FirstOrDefault(c => c.CourseId == id);

            // Nếu không tìm thấy → trả về lỗi 404
            if (course == null) return NotFound();

            // Load dropdown Faculty và chọn sẵn Faculty hiện tại
            BuildFacultyDropdown(course.FacultyId);

            // Trả model về View để hiển thị form
            return View(course);
        }

        // -------------------------------
        // 6. XỬ LÝ SUBMIT EDIT COURSE (POST)
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int CourseId, string CourseName, string Class, int Credits, int FacultyId, DateTime? StartDate, DateTime? EndDate)
        {
            // Kiểm tra dữ liệu required
            if (FacultyId == 0 || string.IsNullOrEmpty(CourseName))
            {
                TempData["ErrorMessage"] = "Please fill all required fields.";

                // Load lại dropdown
                BuildFacultyDropdown(FacultyId);
                return View();
            }

            // Lấy course từ database
            var course = _db.Courses.FirstOrDefault(c => c.CourseId == CourseId);
            if (course == null) return NotFound();

            // Update thông tin
            course.CourseName = CourseName;
            course.Class = Class;
            course.Credits = Credits;
            course.FacultyId = FacultyId;
            course.StartDate = StartDate;
            course.EndDate = EndDate;

            // Lưu thay đổi
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // 7. TẠO DROPDOWN CHO FACULTY
        // -------------------------------
        private void BuildFacultyDropdown(int selectedId = 0)
        {
            // Lấy toàn bộ khoa từ DB
            var faculties = _db.Faculties.ToList();

            // Chuyển thành SelectListItem để đưa xuống View
            ViewBag.Faculties = faculties.Select(f => new SelectListItem
            {
                Value = f.FacultyId.ToString(),
                Text = f.FacultyName,
                Selected = f.FacultyId == selectedId // chọn khoa hiện tại nếu đang edit
            })
            .ToList();
        }

        // -------------------------------
        // 8. XÓA COURSE
        // -------------------------------
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            // Tìm khóa học theo id
            var course = _db.Courses.Find(id);

            if (course == null) return NotFound();

            // Xóa
            _db.Courses.Remove(course);
            _db.SaveChanges();

            TempData["SuccessMessage"] = $"Course '{course.CourseName}' deleted successfully!";

            return RedirectToAction("Index");
        }
    }
}
