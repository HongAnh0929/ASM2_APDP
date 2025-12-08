using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SIMS.Controllers;
using SIMS.DatabaseContext;
using SIMS.DatabaseContext.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SIMS.Test
{
    public class CoursesControllerTest
    {

        private SimDbContext GetDb()
        {
            var options = new DbContextOptionsBuilder<SimDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new SimDbContext(options);
        }

        private CoursesController GetController(SimDbContext db)
        {
            var controller = new CoursesController(db);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());
            return controller;
        }

        private class FakeTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }

        // 1. TEST INDEX()
        [Fact]
        public void Index_ReturnsView_WithCourseList()
        {
            var db = GetDb();
            db.Faculties.Add(new Faculty { FacultyId = 1, FacultyName = "IT", Email = "it@uni.com" });
            db.Courses.Add(new Course { CourseName = "Math", Credits = 3, Class = "A", FacultyId = 1 });
            db.Courses.Add(new Course { CourseName = "Physics", Credits = 4, Class = "B", FacultyId = 1 });
            db.SaveChanges();

            var controller = GetController(db);
            var result = controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<List<Course>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        // 2. TEST ADD POST - VALID DATA
        [Fact]
        public void AddPost_ValidData_AddsCourse_AndRedirects()
        {
            var db = GetDb();
            db.Faculties.Add(new Faculty { FacultyId = 1, FacultyName = "IT", Email = "it@uni.com" });
            db.SaveChanges();

            var controller = GetController(db);

            var result = controller.Add(
                FacultyId: 1,
                CourseName: "Programming",
                Class: "SE001",
                Credits: 3,
                StartDate: DateTime.Now,
                EndDate: DateTime.Now.AddMonths(3)
            ) as RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);

            var added = db.Courses.FirstOrDefault();
            Assert.NotNull(added);
            Assert.Equal("Programming", added.CourseName);
            Assert.Equal("SE001", added.Class);
            Assert.Equal(3, added.Credits);
            Assert.Equal(1, added.FacultyId);
        }

        // 3. TEST ADD POST - INVALID DATA
        [Fact]
        public void AddPost_InvalidData_ReturnsView_WithError()
        {
            var db = GetDb();
            var controller = GetController(db);

            var result = controller.Add(
                FacultyId: 0,
                CourseName: "",
                Class: "SE001",
                Credits: 3,
                StartDate: null,
                EndDate: null
            ) as ViewResult;

            Assert.NotNull(result);
            Assert.NotNull(controller.TempData["ErrorMessage"]);
            Assert.Empty(db.Courses);
        }

        // 4. TEST EDIT (GET)
        [Fact]
        public void EditGet_ExistingCourse_ReturnsView()
        {
            var db = GetDb();
            db.Faculties.Add(new Faculty { FacultyId = 1, FacultyName = "IT", Email = "it@uni.com" });
            db.Courses.Add(new Course { CourseId = 1, CourseName = "Java", Credits = 3, Class = "SE001", FacultyId = 1 });
            db.SaveChanges();

            var controller = GetController(db);
            var result = controller.Edit(1) as ViewResult;

            Assert.NotNull(result);
            var course = Assert.IsType<Course>(result.Model);
            Assert.Equal("Java", course.CourseName);
        }

        // 5. TEST EDIT (POST) SUCCESS
        [Fact]
        public void EditPost_ValidData_UpdatesCourse()
        {
            var db = GetDb();
            db.Faculties.Add(new Faculty { FacultyId = 1, FacultyName = "IT", Email = "it@uni.com" });
            db.Courses.Add(new Course { CourseId = 1, CourseName = "OldName", Credits = 2, Class = "SE001", FacultyId = 1 });
            db.SaveChanges();

            var controller = GetController(db);

            var result = controller.Edit(
                CourseId: 1,
                CourseName: "NewName",
                Class: "SE002",
                Credits: 4,
                FacultyId: 1,
                StartDate: null,
                EndDate: null
            ) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);

            var updated = db.Courses.First();
            Assert.Equal("NewName", updated.CourseName);
            Assert.Equal(4, updated.Credits);
            Assert.Equal("SE002", updated.Class);
        }

        // 6. TEST DELETE
        [Fact]
        public void Delete_RemovesCourse_AndRedirects()
        {
            var db = GetDb();
            db.Faculties.Add(new Faculty { FacultyId = 1, FacultyName = "IT", Email = "it@uni.com" });
            db.Courses.Add(new Course { CourseId = 1, CourseName = "TestCourse", Class = "SE001", Credits = 3, FacultyId = 1 });
            db.SaveChanges();

            var controller = GetController(db);
            var result = controller.Delete(1) as RedirectToActionResult;

            Assert.Equal("Index", result.ActionName);
            Assert.Empty(db.Courses);
        }
    }
}
