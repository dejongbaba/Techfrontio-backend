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
using Course_management.Services;
using Course_management.Interfaces;
using Course_management.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Course_management.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly DataContext _context;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration config, IEmailService emailService, DataContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _emailService = emailService;
            _context = context;
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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return Ok(ApiResponse<PasswordResetResponseDto>.Success(
                    new PasswordResetResponseDto { Success = true, Message = "If the email exists, a password reset link has been sent." },
                    "Password reset email sent", 200));
            }

            // Generate reset token
            var resetToken = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            // Save token to database
            var passwordResetToken = new PasswordResetToken
            {
                UserId = user.Id,
                Token = resetToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(passwordResetToken);
            await _context.SaveChangesAsync();

            // Send email
            var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password";
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, resetUrl);

            return Ok(ApiResponse<PasswordResetResponseDto>.Success(
                new PasswordResetResponseDto { Success = true, Message = "Password reset email sent successfully." },
                "Password reset email sent", 200));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest(ApiResponse.Error("Invalid reset request", 400));
            }

            // Find and validate token
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == dto.Token && t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow);

            if (resetToken == null)
            {
                return BadRequest(ApiResponse.Error("Invalid or expired reset token", 400));
            }

            // Reset password
            var resetPasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!resetPasswordResult.Succeeded)
            {
                return BadRequest(ApiResponse.Error("Failed to reset password", 400));
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                return BadRequest(ApiResponse.Error(errors, 400));
            }

            // Mark token as used
            resetToken.IsUsed = true;
            resetToken.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<PasswordResetResponseDto>.Success(
                new PasswordResetResponseDto { Success = true, Message = "Password reset successfully." },
                "Password reset successful", 200));
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
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
