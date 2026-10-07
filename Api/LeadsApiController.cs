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
    [Route("api/leads")]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsApiController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        /// <summary>
        /// Get paginated & filtered leads list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _leadService.GetLeadsAsync(search, status, priority, scope, page, pageSize);

            var dtos = items.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedToUserId = l.AssignedToUserId,
                AssignedToName = l.AssignedTo?.Name,
                Notes = l.Notes
            });

            return Ok(new { data = dtos, totalCount, page, pageSize });
        }

        /// <summary>
        /// Create a new lead
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var lead = new Lead
            {
                LeadName = model.LeadName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName ?? string.Empty,
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Notes = model.Notes
            };

            string userId = UserScopeHelper.GetUserId(User)!;
            string userName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _leadService.CreateLeadAsync(lead, userId, userName, ip);
            if (!success) return BadRequest(new { error });

            var dto = new LeadDto
            {
                LeadId = created!.LeadId,
                LeadCode = created.LeadCode,
                LeadName = created.LeadName,
                Email = created.Email,
                Phone = created.Phone,
                CompanyName = created.CompanyName,
                Source = created.Source,
                Status = created.Status,
                Priority = created.Priority,
                ExpectedValue = created.ExpectedValue,
                CreatedDate = created.CreatedDate,
                AssignedToUserId = created.AssignedToUserId,
                Notes = created.Notes
            };

            return CreatedAtAction(nameof(GetLeads), new { id = dto.LeadId }, dto);
        }
    }
}
