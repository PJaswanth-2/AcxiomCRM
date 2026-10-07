using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services.Interfaces;

namespace AcxiomCRM.Api
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersApiController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Get paginated & filtered customers list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _customerService.GetCustomersAsync(search, status, scope, page, pageSize);

            var dtos = items.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedToUserId = c.AssignedToUserId,
                AssignedToName = c.AssignedTo?.Name,
                Notes = c.Notes
            });

            return Ok(new { data = dtos, totalCount, page, pageSize });
        }

        /// <summary>
        /// Get customer by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var c = await _customerService.GetCustomerByIdAsync(id);
            if (c == null) return NotFound(new { message = "Customer not found." });

            if (UserScopeHelper.IsSalesExecutive(User) && c.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return StatusCode(403, new { message = "Access denied." });
            }

            var dto = new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedToUserId = c.AssignedToUserId,
                AssignedToName = c.AssignedTo?.Name,
                Notes = c.Notes
            };

            return Ok(dto);
        }

        /// <summary>
        /// Create a new customer
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Address = model.Address ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Notes = model.Notes
            };

            string userId = UserScopeHelper.GetUserId(User)!;
            string userName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _customerService.CreateCustomerAsync(customer, userId, userName, ip);

            if (!success) return BadRequest(new { error });

            var dto = new CustomerDto
            {
                CustomerId = created!.CustomerId,
                CustomerCode = created.CustomerCode,
                CustomerName = created.CustomerName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Address = created.Address,
                City = created.City,
                State = created.State,
                Status = created.Status,
                CreatedDate = created.CreatedDate,
                AssignedToUserId = created.AssignedToUserId,
                Notes = created.Notes
            };

            return CreatedAtAction(nameof(GetCustomerById), new { id = dto.CustomerId }, dto);
        }

        /// <summary>
        /// Update an existing customer
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _customerService.GetCustomerByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Customer not found." });

            if (UserScopeHelper.IsSalesExecutive(User) && existing.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return StatusCode(403, new { message = "Access denied." });
            }

            var customer = new Customer
            {
                CustomerId = id,
                CustomerCode = existing.CustomerCode,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Address = model.Address ?? string.Empty,
                City = model.City ?? string.Empty,
                State = model.State ?? string.Empty,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId ?? existing.AssignedToUserId,
                Notes = model.Notes
            };

            string userId = UserScopeHelper.GetUserId(User)!;
            string userName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _customerService.UpdateCustomerAsync(customer, userId, userName, ip);
            if (!success) return BadRequest(new { error });

            return Ok(new { message = "Customer updated successfully." });
        }

        /// <summary>
        /// Deactivate/Delete customer
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var existing = await _customerService.GetCustomerByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Customer not found." });

            if (UserScopeHelper.IsSalesExecutive(User) && existing.AssignedToUserId != UserScopeHelper.GetUserId(User))
            {
                return StatusCode(403, new { message = "Access denied." });
            }

            string userId = UserScopeHelper.GetUserId(User)!;
            string userName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error) = await _customerService.DeleteCustomerAsync(id, userId, userName, ip);
            if (!success) return BadRequest(new { error });

            return Ok(new { message = "Customer deactivated successfully." });
        }
    }
}
