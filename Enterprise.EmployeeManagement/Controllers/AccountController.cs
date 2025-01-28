using Enterprise.EmployeeManagement.DAL.Context;
using Enterprise.EmployeeManagement.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Enterprise.EmployeeManagement.core.Utilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogInformation("AccountController initialized.");
        }

        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogInformation("Login page requested.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            _logger.LogInformation("Login attempt with email: {Email}", model.Email);
            if (ModelState.IsValid)
            {

                // Hash the entered password
                string hashedPassword = PasswordHasher.HashPassword(model.Password);

                // Verify against the hashed password in the database
                var employee = _context.Employees.SingleOrDefault(e => e.Email == model.Email && e.Password == hashedPassword);
                if (employee != null)
                {
                    _logger.LogInformation("Successful login for user: {Email}", model.Email);
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, employee.Email),
                        new Claim(ClaimTypes.Role, employee.Role.ToString()),
                        new Claim("EmployeeId", employee.Id.ToString()) // Custom claim for Employee ID
                    };
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // Create authentication properties
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // Persistent cookie (remains after the session ends)
                        ExpiresUtc = System.DateTime.UtcNow.AddMinutes(30) // Set expiration time
                    };

                    // Sign in the user
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                    HttpContext.Session.SetString("UserEmail", employee.Email);
                    _logger.LogInformation("Redirecting user {Email} to Employee Management Page", model.Email);

                    // Redirect to Employee Management Page
                    return new RedirectResult(Url.Action("Index", "Employee"));


                }
                _logger.LogWarning("Invalid login attempt with email: {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User logging out");
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the Login page
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
        [HttpGet("api/user/current")]
        public IActionResult GetCurrentUser()
        {
            // Retrieve user claims
            var currentUser = new
            {
                Name = User.Identity.Name, 
                Role = User.FindFirstValue(ClaimTypes.Role) 
            };
            _logger.LogInformation("Current user info: Name - {Name}, Role - {Role}", currentUser.Name, currentUser.Role);
            return Ok(currentUser);
        }

       

    }
}
