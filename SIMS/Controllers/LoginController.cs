using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SIMS.Models;
using SIMS.Services;

namespace SIMS.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserService _userService;

        public LoginController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            // 1️⃣ Validate model rỗng
            if (!ModelState.IsValid)
                return View(model);

            // ================= USERNAME =================
            if (string.IsNullOrWhiteSpace(model.Username) || model.Username.Length != 8)
            {
                ModelState.AddModelError(
                    "Username",
                    "Username must be exactly 8 characters."
                );
            }

            // ================= PASSWORD =================
            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
            {
                ModelState.AddModelError(
                    "Password",
                    "Password must be at least 8 characters long."
                );
            }
            else if (
                !Regex.IsMatch(model.Password, @"[A-Z]") ||     // chữ hoa
                !Regex.IsMatch(model.Password, @"[a-z]") ||     // chữ thường
                !Regex.IsMatch(model.Password, @"\d") ||        // số
                !Regex.IsMatch(model.Password, @"[!@#$%^&*]")   // ký tự đặc biệt
            )
            {
                ModelState.AddModelError(
                    "Password",
                    "Password must contain uppercase, lowercase, number and special character."
                );
            }

            // Nếu có lỗi → trả view
            if (!ModelState.IsValid)
                return View(model);

            // ====== CHECK ACCOUNT ======
            var user = await _userService.LoginUserAsync(model.Username, model.Password);

            if (user == null)
            {
                ViewData["InvalidAccount"] = "Invalid username or password.";
                return View(model);
            }

            // ====== SIGN IN (GIỮ NGUYÊN) ======
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return RedirectToAction("Index", "Dashboard");
        }

        // ====== LOGOUT (GIỮ NGUYÊN THEO BẠN) ======
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            return RedirectToAction("Index", "Login");
        }
    }
}
