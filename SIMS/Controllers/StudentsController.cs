// ===================== StudentsController Explained =====================
// Mình đã thêm comment giải thích chi tiết từng phần trong code

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SIMS.Controllers
{
    public class StudentsController : Controller
    {
        private readonly SimDbContext _db; // DbContext dùng để truy cập database

        public StudentsController(SimDbContext db)
        {
            _db = db; // inject DbContext
        }

        // ====================== STUDENT VIEW THEIR OWN INFO =======================
        [Authorize(Roles = "Student")]
        public IActionResult Info()
        {
            // Lấy UserId từ token đăng nhập
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Lấy student gắn với user
            var student = _db.Students
                .Include(s => s.User) // load thêm bảng User
                .FirstOrDefault(s => s.UserId == userId);

            return View(student);
        }

        // ====================== LIST STUDENTS (SEARCH) =======================
        [Authorize(Roles = "Admin, Faculty, Student")]
        public async Task<IActionResult> Index(string search)
        {
            // Query ban đầu gồm Students + User
            var studentsQuery = _db.Students.Include(s => s.User).AsQueryable();

            // Nếu có tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();

                studentsQuery = studentsQuery.Where(s =>
                    s.FullName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search) ||
                    s.Class.ToLower().Contains(search)
                );
            }

            var students = await studentsQuery.ToListAsync();

            ViewBag.SearchTerm = search; // gửi lại text search ra view
            return View(students);
        }

        // ====================== ADD STUDENT (GET) =======================
        [Authorize(Roles = "Admin")]
        public IActionResult Add() => View(new Student()); // trả về form rỗng


        // ====================== ADD STUDENT (POST) =======================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Add(string FullName, string Major, DateTime Dob,
                                 float GPA, string AcademicStanding,
                                 string Class, string Username,
                                 string Password, string Email, string Gender)
        {
            // Kiểm tra username có tồn tại chưa
            if (_db.Users.Any(u => u.Username == Username))
            {
                TempData["ErrorMessage"] = "Username already exists!";
                return RedirectToAction("Add");
            }

            // Tạo user trước vì Student cần UserId
            var newUser = new User
            {
                Username = Username,
                Email = Email,
                HashPassword = Password, 
                Role = "Student"
            };

            _db.Users.Add(newUser);
            _db.SaveChanges(); // lưu lại để có Id mới

            // Tạo student
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
                UserId = newUser.Id // gán user
            };

            _db.Students.Add(student);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Student created successfully!";
            return RedirectToAction("Index");
        }

        // ====================== EDIT STUDENT (GET) =======================
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            // Lấy student cần sửa kèm user
            var student = _db.Students.Include(s => s.User)
                                      .FirstOrDefault(s => s.StudentId == id);

            if (student == null) return NotFound();
            return View(student);
        }

        // ====================== EDIT STUDENT (POST) =======================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Student student)
        {
            // Lấy thông tin cũ để update
            var existing = _db.Students.Find(student.StudentId);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Student not found!";
                return RedirectToAction("Index");
            }

            // Cập nhật
            existing.FullName = student.FullName;
            existing.Email = student.Email;
            existing.Gender = student.Gender;
            existing.Major = student.Major;
            existing.Dob = student.Dob;
            existing.GPA = student.GPA;
            existing.AcademicStanding = student.AcademicStanding;
            existing.Class = student.Class;

            _db.SaveChanges();

            TempData["SuccessMessage"] = "Student updated successfully!";
            return RedirectToAction("Index");
        }

        // ====================== DELETE STUDENT =======================
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var student = _db.Students.Find(id);
            if (student == null) return NotFound();

            _db.Students.Remove(student);
            _db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}