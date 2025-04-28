using RetailCycleShopAPI.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using RetailCycleShopAPI.module;
using RetailCycleShopAPI.Services;
using RetailCycleShopAPI.Interfaces;
using RetailCycleShopAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IInvitationService _invitationService;
        private readonly ApplicationDbContext _context;
        private JwtSecurityToken GenerateJwtToken(ApplicationUser user, string role)
        {
            var authClaims = new List<Claim>
        {
           new(ClaimTypes.NameIdentifier, user.Id), 
        new(ClaimTypes.Name, user.UserName),    
        new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var authSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(
                    authSigningKey,
                    SecurityAlgorithms.HmacSha256));
        }


        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IEmailService emailService,
            IInvitationService invitationService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
            _invitationService = invitationService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Validate input
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid registration data.");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest("User already exists.");
            }

            // Check if email was invited (unless coming from setup flow)
            var isInvited = await _invitationService.IsEmailInvited(model.Email);
            if (!isInvited)
            {
                return BadRequest("This email hasn't been invited by an admin.");
            }

            // Create and register the user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Assign role
            await _userManager.AddToRoleAsync(user, model.Role);

            // Mark invitation as completed
            await _invitationService.MarkAsRegistered(model.Email);

            return Ok(new { Message = "User registered successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid login data.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles.FirstOrDefault()!);

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            });
        }

        [HttpPost("admin-create-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCreateUser([FromBody] AdminCreateUserModel model)
        {
            // Validate input
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Role))
            {
                return BadRequest("Email and role are required.");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest("User already exists.");
            }

            // Create invitation
            try
            {
                var invitedUser = await _invitationService.CreateInvitation(model);
                return Ok(new
                {
                    Message = "Invitation sent successfully",
                    Email = invitedUser.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to send invitation", Error = ex.Message });
            }
        }

        [HttpGet("validate-invitation")]
        public async Task<IActionResult> ValidateInvitation([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            var isValid = await _invitationService.IsEmailInvited(email);
            return Ok(new { IsValid = isValid });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist for security reasons
                return Ok(new { Message = "If your email is registered, you'll receive a password reset link." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Create reset link (frontend URL)
            var resetLink = $"{_configuration["Frontend:BaseUrl"]}/reset-password?email={user.Email}&token={encodedToken}";

            // Send email
            await _emailService.SendEmailAsync(
                user.Email,
                "Reset Your Password",
                $"Please reset your password by clicking here: {resetLink}");

            return Ok(new { Message = "If your email is registered, you'll receive a password reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (string.IsNullOrEmpty(model.Email) ||
                string.IsNullOrEmpty(model.Token) ||
                string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Email, token and new password are required.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist for security reasons
                return Ok(new { Message = "Password reset successfully." });
            }

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Password reset successfully." });
        }
        [HttpPost("setup-password")]
        public async Task<IActionResult> SetupPassword([FromBody] SetupPasswordModel model)
        {
            if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Token and password are required.");
            }

            try
            {
                // Validate the invitation token
                var invitation = await _invitationService.GetInvitationByToken(model.Token);
                if (invitation == null)
                {
                    return BadRequest("Invalid or expired invitation token.");
                }

                if (invitation.IsRegistered)
                {
                    return BadRequest("This invitation has already been used.");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
                if (existingUser != null)
                {
                    return BadRequest("User already exists.");
                }

                // Create the user
                var user = new ApplicationUser
                {
                    UserName = invitation.Email,
                    Email = invitation.Email,
                    FullName = invitation.FullName,
                    Role = invitation.Role,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (!createResult.Succeeded)
                {
                    return BadRequest(createResult.Errors);
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, invitation.Role);

                // Mark invitation as completed
                await _invitationService.MarkAsRegistered(invitation.Email);

                return Ok(new { Message = "Account setup successfully. You can now login." });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in SetupPassword: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while setting up your password.", Error = ex.Message });
            }
        }
    }
}