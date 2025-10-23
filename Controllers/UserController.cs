using Microsoft.AspNetCore.Mvc;
using BarterSystem.Data;
using BarterSystem.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BarterSystem.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                string hashedPassword = HashPassword(password);

                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Username == username && u.Password == hashedPassword);

                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("Username", user.Username);
                    TempData["Success"] = $"Welcome back, {user.Username}!";
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "Invalid username or password.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during login.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password, string displayName, string email)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    ViewBag.Error = "Username already exists.";
                    return View();
                }

                string hashedPassword = HashPassword(password);

                var newUser = new User
                {
                    Username = username,
                    Password = hashedPassword,
                    DisplayName = displayName,
                    Email = email
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Account created successfully! Please login.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "An error occurred during registration.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Message"] = "You have successfully logged out.";
            return RedirectToAction("Login", "User");
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }
    }
}