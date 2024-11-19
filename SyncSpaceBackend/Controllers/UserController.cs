using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Context;
using WebAPI.Models;
using SyncSpaceBackend.Helper;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System;
using Google.Apis.Auth;
using SyncSpaceBackend.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace SyncSpaceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _authContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(AppDbContext appDbcontext, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _authContext = appDbcontext;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest(new { Message = "Invalid user data" });

            var user = await _authContext.Users
                .FirstOrDefaultAsync(x => x.Username == userObj.Username);

            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (!PasswordHasher.VerifyPassword(userObj.Password, user.Password))
                return BadRequest(new { Message = "Incorrect password" });

            user.Token = CreateJwt(user);
            return Ok(new
            {
                Token = user.Token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.ProfilePicture,
                    user.CreatedAt,
                    user.UpdatedAt
                },
                Message = "Login successful"
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
                if (userObj == null)
                    return BadRequest(new { Message = "Invalid user data" });

                // Check if user already exists
                if (await _authContext.Users.AnyAsync(x => x.Username == userObj.Username))
                    return BadRequest(new { Message = "Username already exists" });

                // Check if email already exists
                if (await _authContext.Users.AnyAsync(x => x.Email == userObj.Email))
                    return BadRequest(new { Message = "Email already exists" });

                // Hash password
                userObj.Password = PasswordHasher.HashPassword(userObj.Password);
                userObj.Role = "User";

                userObj.CreatedAt = DateTime.Now;

                await _authContext.Users.AddAsync(userObj);
                await _authContext.SaveChangesAsync();

                return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("google-authenticate")]
        public async Task<IActionResult> GoogleAuthenticate([FromBody] GoogleAuthRequest request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Google:ClientId"] }
                });

                var user = await _authContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    if (request.IsSignUp)
                    {
                        // Create a new user for sign-up
                        user = new User
                        {
                            Email = payload.Email,
                            Username = payload.Email,
                            FirstName = payload.GivenName,
                            LastName = payload.FamilyName,
                            Role = "User",
                            CreatedAt = DateTime.Now
                        };
                        await _authContext.Users.AddAsync(user);
                        await _authContext.SaveChangesAsync();
                    }
                    else
                    {
                        // User doesn't exist and this is a login attempt
                        return NotFound(new { Message = "User not found. Please sign up first." });
                    }
                }
                else if (request.IsSignUp)
                {
                    // User exists but this is a sign-up attempt
                    return Conflict(new { Message = "User already exists. Please log in instead." });
                }

                var token = CreateJwt(user);
                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.Username,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.Role,
                        user.ProfilePicture
                    },
                    Message = request.IsSignUp ? "Google sign-up successful" : "Google login successful"
                });
            }
            catch (InvalidJwtException ex)
            {
                return BadRequest(new { Message = "Invalid Google token" });
            }
        }

        [HttpPut("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileRequest request)
        {
            try
            {
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = Convert.ToInt32(userIdString);
                var user = await _authContext.Users.FindAsync(userId);

                if (user == null)
                    return NotFound(new { Message = "User not found" });

                // Verify current password
                if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.Password))
                    return BadRequest(new { Message = "Current password is incorrect" });

                // Update basic info
                user.FirstName = request.FirstName ?? user.FirstName;
                user.LastName = request.LastName ?? user.LastName;
                user.Email = request.Email ?? user.Email;
                user.Username = request.Username ?? user.Username;

                // Update timestamp
                user.UpdatedAt = DateTime.UtcNow;

                // Update password if provided
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    user.Password = PasswordHasher.HashPassword(request.NewPassword);
                }

                // Handle profile picture upload if provided
                if (request.ProfilePicture != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.ProfilePicture.FileName)}";
                    var filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "uploads", "profiles", fileName);

                    // Ensure the profiles folder exists
                    var directoryPath = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Save the file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.ProfilePicture.CopyToAsync(stream);
                    }

                    // Update the profile picture path
                    user.ProfilePicture = $"/uploads/profiles/{fileName}";
                }

                await _authContext.SaveChangesAsync();

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.ProfilePicture,
                    user.CreatedAt,
                    user.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the profile", Error = ex.Message });
            }
        }

        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")

            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            return jwtTokenHandler.WriteToken(token);
        }
    }
}