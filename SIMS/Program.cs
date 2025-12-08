using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SIMS.DatabaseContext;
using SIMS.Interfaces;
using SIMS.Repositories;
using SIMS.Services;

namespace SIMS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            /* --------------------------------------------------------
             * 1. CẤU HÌNH DATABASE (DI: Dependency Injection)
             * --------------------------------------------------------
             * AddDbContext để đăng ký SimDbContext vào hệ thống DI.
             * options.UseSqlServer(...) để chỉ định dùng SQL Server.
             * ConnectionString "Default" lấy từ appsettings.json
             --------------------------------------------------------- */
            // configure to database
            builder.Services.AddDbContext<SimDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

            /* --------------------------------------------------------
             * 2. ĐĂNG KÝ REPOSITORY + SERVICE
             * --------------------------------------------------------
             * AddScoped: mỗi request tạo một instance mới
             * IUserRepository → UserRepository (Interface → Implementation)
             * UserService và FacultyService cũng được Inject
             --------------------------------------------------------- */
            // configure Services
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<FacultyService>();

            /* --------------------------------------------------------
             * 3. Thêm MVC (Controllers + Razor Views)
             --------------------------------------------------------- */
            // Add services to the container.
            builder.Services.AddControllersWithViews();

            /* --------------------------------------------------------
             * 4. ĐĂNG KÝ HttpContextAccessor
             * Dùng để truy cập HttpContext trong các service khác.
             --------------------------------------------------------- */
            // cau hinh role login
            builder.Services.AddHttpContextAccessor();

            /* --------------------------------------------------------
             * 5. CẤU HÌNH COOKIE AUTHENTICATION (Đăng nhập bằng cookie)
             * --------------------------------------------------------
             * LoginPath: khi chưa login sẽ redirect sang /Login
             * AccessDeniedPath: khi không có quyền → /Auth/AccessDenied
             --------------------------------------------------------- */
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
            {
                options.LoginPath = "/Login";
                options.AccessDeniedPath = "/Auth/AccessDenied";
            });

            /* --------------------------------------------------------
            * 6. CẤU HÌNH AUTHORIZATION (Role-based)
            * --------------------------------------------------------
            * Tạo policy cho từng loại tài khoản: Admin, Student, Faculty
            --------------------------------------------------------- */
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
                options.AddPolicy("FacultyOnly", policy => policy.RequireRole("Faculty"));
            });

            // 1. Thêm SessionStateTempDataProvider cho TempData thay vì Cookie
            builder.Services.AddControllersWithViews()
                   .AddSessionStateTempDataProvider(); // <-- đây

            // 2. Thêm Session
            builder.Services.AddSession();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts(); // tăng bảo mật HTTPS
            }

            app.UseHttpsRedirection();// Tự động redirect HTTP → HTTPS
            app.UseStaticFiles(); // Cho phép truy cập wwwroot

            app.UseRouting(); // Bật định tuyến

            app.UseAuthentication(); // Kích hoạt Authentication

            app.UseSession(); // Bật Session cho app

            app.UseAuthorization(); // Bật Session cho app

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
