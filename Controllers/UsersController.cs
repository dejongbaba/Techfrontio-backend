using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Course_management.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System;
using Course_management.Dto;
using Microsoft.AspNetCore.Authorization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Course_management.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _config;

        public UsersController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
        }

        // GET: api/<UsersController> - Admin only
        [HttpGet]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users.ToList();
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role
            }).ToList();
            
            return Ok(ApiResponse<List<UserDto>>.Success(userDtos, "Users retrieved successfully", 200));
        }

        // GET api/<UsersController>/5 - Admin only
        [HttpGet("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse.Error("User not found", 404));

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
            
            return Ok(ApiResponse<UserDto>.Success(userDto, "User retrieved successfully", 200));
        }
        
        // GET api/users/profile - Current authenticated user
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Error("User not authenticated", 401));
                
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(ApiResponse.Error("User not found", 404));

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
            
            return Ok(ApiResponse<UserDto>.Success(userDto, "Profile retrieved successfully", 200));
        }

        // PUT api/<UsersController>/5 - Admin only
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse.Error("User not found", 404));

            user.FullName = userDto.FullName;
            // Only update role if it's different and valid
            if (!string.IsNullOrEmpty(userDto.Role) && user.Role != userDto.Role)
            {
                var validRoles = new[] { "Admin", "Tutor", "Student" };
                if (!validRoles.Contains(userDto.Role))
                    return BadRequest(ApiResponse.Error("Invalid role specified", 400));
                    
                // Remove from old role
                await _userManager.RemoveFromRoleAsync(user, user.Role);
                // Add to new role
                await _userManager.AddToRoleAsync(user, userDto.Role);
                user.Role = userDto.Role;
            }
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(ApiResponse.Error("Failed to update user", 400));
                
            return Ok(ApiResponse.Success("User updated successfully", 200));
        }

        // DELETE api/<UsersController>/5 - Admin only
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(ApiResponse.Error("User not found", 404));

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(ApiResponse.Error("Failed to delete user", 400));
                
            return Ok(ApiResponse.Success("User deleted successfully", 200));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Validate role
            var validRoles = new[] { "Admin", "Tutor", "Student" };
            if (!validRoles.Contains(dto.Role))
                return BadRequest(ApiResponse.Error("Invalid role specified", 400));
                
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(ApiResponse.Error("Email already registered", 400));
                
            var user = new User { UserName = dto.Email, Email = dto.Email, FullName = dto.FullName, Role = dto.Role };
            var result = await _userManager.CreateAsync(user, dto.Password);
            
            if (!result.Succeeded) 
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse.Error(errors, 400));
            }
            
            await _userManager.AddToRoleAsync(user, dto.Role);
            
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
            
            return Ok(ApiResponse<UserDto>.Success(userDto, "User registered successfully", 201));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized(ApiResponse.Error("Invalid email or password", 401));
            
            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized(ApiResponse.Error("Invalid email or password", 401));
            
            var token = GenerateJwtToken(user);
            
            return Ok(ApiResponse<object>.Success(new { token }, "Login successful", 200));
        }

        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null) return Unauthorized(ApiResponse.Error("External login information not found", 401));
            
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                var token = GenerateJwtToken(user);
                return Ok(ApiResponse<object>.Success(new { token }, "Google login successful", 200));
            }
            
            // If user does not exist, create
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var userNew = new User { UserName = email, Email = email, FullName = name, Role = "Student" };
            
            var result = await _userManager.CreateAsync(userNew);
            if (!result.Succeeded) 
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse.Error(errors, 400));
            }
            
            await _userManager.AddToRoleAsync(userNew, "Student");
            await _userManager.AddLoginAsync(userNew, info);
            
            var jwt = GenerateJwtToken(userNew);
            return Ok(ApiResponse<object>.Success(new { token = jwt }, "Google account registered and logged in", 201));
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "Student")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } // Student, Tutor, Admin
    }
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
