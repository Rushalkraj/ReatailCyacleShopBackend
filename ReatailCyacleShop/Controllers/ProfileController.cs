// Controllers/ProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RetailCycleShopAPI.Models.Identity;
using System.Security.Claims;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            // Try getting user by both NameIdentifier and Email as fallback
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Name); // Corresponds to UserName in JWT

            ApplicationUser? user = null;

            if (!string.IsNullOrEmpty(userId))
            {
                user = await _userManager.FindByIdAsync(userId);
            }

            // Fallback to email lookup if user not found by ID
            if (user == null && !string.IsNullOrEmpty(userEmail))
            {
                user = await _userManager.FindByEmailAsync(userEmail);
            }

            if (user == null)
            {
                return NotFound(new
                {
                    Message = "User not found in database",
                    UserId = userId,
                    UserEmail = userEmail
                });
            }

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Update full name if provided
            if (!string.IsNullOrEmpty(request.FullName))
            {
                user.FullName = request.FullName;
            }

            // Update email if provided
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(request.Email);
                if (emailExists != null)
                {
                    return BadRequest("Email already in use");
                }

                user.Email = request.Email;
                user.UserName = request.Email;
            }

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword) && !string.IsNullOrEmpty(request.CurrentPassword))
            {
                var result = await _userManager.ChangePasswordAsync(
                    user,
                    request.CurrentPassword,
                    request.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(updateResult.Errors);
            }

            return Ok(new { Message = "Profile updated successfully" });
        }
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}