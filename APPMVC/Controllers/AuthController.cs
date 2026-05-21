using Microsoft.AspNetCore.Mvc;
using APPMVC.Models;
using APPMVC.Repositories;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace APPMVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserRepositories _userRepository;

        public AuthController(UserRepositories userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

        [HttpPost]
        public IActionResult Register(string name, string email, string phoneNumber, string password, string role)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please fill in all fields.");
                return View();
            }

            var user = new User
            {
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber,
                roles = [Enum.TryParse<Role>(role, out var parsedRole) ? parsedRole : Role.USER]
            };

            if (_userRepository.Register(user, password))
            {
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Email already exists.");
            return View();
        }

        [HttpGet]
        public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please enter email and password.");
                return View();
            }

            var user = _userRepository.Login(email, password);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Email),
                new("DisplayName", user.Name)
            };
            claims.AddRange(user.roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties { IsPersistent = true });
            
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
