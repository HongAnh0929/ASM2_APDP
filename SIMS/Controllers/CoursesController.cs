using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System;
using System.Linq;

namespace SIMS.Controllers
{
    [Authorize(Roles = "Admin, Faculty, Student")]
    public class CoursesController : Controller
    {
        private readonly SimDbContext _db;

        public CoursesController(SimDbContext db)
        {
            _db = db;
        }

        // -------------------------------
        // 1. LIST COURSES
        // -------------------------------
        [HttpGet]
        public IActionResult Index(string search)
        {
            var courses = _db.Courses.Include(c => c.Faculty).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                courses = courses.Where(c =>
                    c.CourseName.ToLower().Contains(search) ||
                    c.Class.ToLower().Contains(search) ||
                    c.Faculty.FacultyName.ToLower().Contains(search));
            }

            return View(courses.ToList());
        }

        // -------------------------------
        // 2. GET ADD COURSE FORM
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Add()
        {
            LoadFacultiesDropdown();
            return View();
        }

        // -------------------------------
        // 3. POST ADD COURSE
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int FacultyId, string CourseName, string Class, int Credits, DateTime? StartDate, DateTime? EndDate)
        {
            if (FacultyId == 0 || string.IsNullOrEmpty(CourseName))
            {
                TempData["ErrorMessage"] = "Please select a faculty and enter course name.";
                LoadFacultiesDropdown(); // Load lại dropdown khi lỗi
                return View();
            }

            var course = new Course
            {
                CourseName = CourseName,
                Credits = Credits,
                Class = Class,
                FacultyId = FacultyId,
                StartDate = StartDate,
                EndDate = EndDate
            };

            _db.Courses.Add(course);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Course added successfully!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // 4. GET EDIT COURSE FORM
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var course = _db.Courses.FirstOrDefault(c => c.CourseId == id);
            if (course == null) return NotFound();

            LoadFacultiesDropdown(course.FacultyId);
            return View(course);
        }

        // -------------------------------
        // 5. POST EDIT COURSE
        // -------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int CourseId, string CourseName, string Class, int Credits, int FacultyId, DateTime? StartDate, DateTime? EndDate)
        {
            if (FacultyId == 0 || string.IsNullOrEmpty(CourseName))
            {
                TempData["ErrorMessage"] = "Please fill all required fields.";
                LoadFacultiesDropdown(FacultyId);
                return View();
            }

            var course = _db.Courses.FirstOrDefault(c => c.CourseId == CourseId);
            if (course == null) return NotFound();

            course.CourseName = CourseName;
            course.Class = Class;
            course.Credits = Credits;
            course.FacultyId = FacultyId;
            course.StartDate = StartDate;
            course.EndDate = EndDate;

            _db.SaveChanges();
            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // 6. DELETE COURSE
        // -------------------------------
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var course = _db.Courses.Find(id);
            if (course == null) return NotFound();

            _db.Courses.Remove(course);
            _db.SaveChanges();

            TempData["SuccessMessage"] = $"Course '{course.CourseName}' deleted successfully!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // HELPER: LOAD FACULTY DROPDOWN
        // -------------------------------
        private void LoadFacultiesDropdown(int selectedId = 0)
        {
            var faculties = _db.Faculties.ToList();
            ViewBag.Faculties = faculties.Select(f => new SelectListItem
            {
                Value = f.FacultyId.ToString(),
                Text = f.FacultyName,
                Selected = f.FacultyId == selectedId
            }).ToList();
        }
    }
}
