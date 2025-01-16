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

namespace Enterprise.EmployeeManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (ModelState.IsValid)
            {
                // Hash the entered password
                string hashedPassword = PasswordHasher.HashPassword(model.Password);

                // Verify against the hashed password in the database
                var employee = _context.Employees.SingleOrDefault(e => e.Email == model.Email && e.Password == hashedPassword);
                if (employee != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, employee.Email),
                        new Claim(ClaimTypes.Role, employee.Role),
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

                    // Redirect to Employee Management Page
                    return new RedirectResult(Url.Action("Index", "Employee"));


                }

                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Sign out the user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect to the Login page
            return RedirectToAction("Login", "Account");
        }

        //[HttpGet]
        //public IActionResult AccessDenied()
        //{
        //    return View(); // Optional: Create a view to show an "Access Denied" message.
        //}

    }
}
