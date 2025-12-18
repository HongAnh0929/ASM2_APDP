using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext.Entities;

namespace SIMS.DatabaseContext
{
    public class SimDbContext : DbContext
    {
        public SimDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<FacultyCourse> FacultyCourses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ================= USER =================
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();

                entity.Property(u => u.Status).HasDefaultValue(1);
                entity.Property(u => u.Role).HasDefaultValue("Student");
            });

            // ================= STUDENT =================
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // ================= FACULTY =================
            modelBuilder.Entity<Faculty>()
                .HasOne(f => f.User)
                .WithOne(u => u.Faculty)
                .HasForeignKey<Faculty>(f => f.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // ================= FACULTY COURSE =================
            modelBuilder.Entity<FacultyCourse>()
                .HasOne(fc => fc.Faculty)
                .WithMany(f => f.FacultyCourses)
                .HasForeignKey(fc => fc.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FacultyCourse>()
                .HasOne(fc => fc.Course)
                .WithMany(c => c.FacultyCourses)
                .HasForeignKey(fc => fc.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FacultyCourse>()
                .HasIndex(fc => new { fc.FacultyId, fc.CourseId })
                .IsUnique();

            // ================= ENROLLMENT =================
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ================= SCHEDULE =================
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Course)
                .WithMany()
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Faculty)
                .WithMany()
                .HasForeignKey(s => s.FacultyId)
                .OnDelete(DeleteBehavior.Cascade);

            // ================= ATTENDANCE =================
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Course)
                .WithMany()
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Faculty)
                .WithMany()
                .HasForeignKey(a => a.FacultyId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
