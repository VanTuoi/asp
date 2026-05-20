using Microsoft.AspNetCore.Mvc;
using APPMVC.Models;
using APPMVC.Repositories;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace APPMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepositories _userRepository;

        public AccountController(UserRepositories userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Register(string name, string userName, string password, string role)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please fill in all fields.");
                return View();
            }

            var user = new User
            {
                Name = name,
                UserName = userName,
                roles = new List<Role> { Enum.TryParse<Role>(role, out var parsedRole) ? parsedRole : Role.USER }
            };

            bool success = _userRepository.Register(user, password);
            if (success)
            {
                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Username already exists.");
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string userName, string password)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Please enter username and password.");
                return View();
            }

            var user = _userRepository.Login(userName, password);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("DisplayName", user.Name)
                };

                foreach (var role in user.roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
