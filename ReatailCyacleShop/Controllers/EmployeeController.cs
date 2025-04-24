// Controllers/EmployeeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RetailCycleShopAPI.Models.Identity;
using RetailCycleShopAPI.module;
using System.ComponentModel.DataAnnotations;

namespace RetailCycleShopAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class EmployeeController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/employee
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var result = employees.Select(e => new EmployeeResponse
            {
                Id = e.Id,
                FullName = e.FullName,
                Email = e.Email,
                CreatedDate = e.CreatedDate
            }).OrderBy(e => e.FullName).ToList();

            return Ok(result);
        }

        // GET: api/employee/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployee(string id)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
                return NotFound("Employee not found");

            return Ok(new EmployeeResponse
            {
                Id = employee.Id,
                FullName = employee.FullName,
                Email = employee.Email,
                CreatedDate = employee.CreatedDate
            });
        }

        // PUT: api/employee/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeRequest request)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
                return NotFound("Employee not found");

            // Update full name
            if (!string.IsNullOrEmpty(request.FullName))
            {
                employee.FullName = request.FullName;
            }

            // Update email if changed
            if (!string.IsNullOrEmpty(request.Email) && request.Email != employee.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(request.Email);
                if (emailExists != null)
                {
                    return BadRequest("Email already in use");
                }

                employee.Email = request.Email;
                employee.UserName = request.Email;
            }

            var result = await _userManager.UpdateAsync(employee);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Employee updated successfully" });
        }

        // DELETE: api/employee/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
                return NotFound("Employee not found");

            var result = await _userManager.DeleteAsync(employee);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }

    public class UpdateEmployeeRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}