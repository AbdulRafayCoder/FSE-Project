using Microsoft.AspNetCore.Mvc;
using FSEBACKEND.Data;
using FSEBACKEND.Models;

namespace FSEBACKEND.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest login)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == login.Email && u.Password == login.Password);
            if (user == null)
            {
                return Ok(new { Status = false, Email = (string?)null, Role = (string?)null });
            }
            return Ok(new { Status = true, Email = user.Email, Role = user.Role });
        }

        [HttpPost("adduser")]
        public IActionResult AddUser(AddUserRequest newUser)
        {
            if (string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(newUser.Password))
            {
                return BadRequest("Email and password are required.");
            }

            if (_context.Users.Any(u => u.Email == newUser.Email))
            {
                return BadRequest("A user with this email already exists.");
            }

            var user = new User(newUser.Email, newUser.Password, "Student");
            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { Status = "User added", User = user });
        }
    }
}