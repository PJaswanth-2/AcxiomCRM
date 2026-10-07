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
    [Route("api/followups")]
    [Authorize]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;

        public FollowUpsApiController(IFollowUpService followUpService)
        {
            _followUpService = followUpService;
        }

        /// <summary>
        /// Get followups list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _followUpService.GetFollowUpsAsync(search, status, type, null, null, null, scope, page, pageSize);

            var dtos = items.Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer?.CustomerName,
                LeadId = f.LeadId,
                LeadName = f.Lead?.LeadName,
                OpportunityId = f.OpportunityId,
                OpportunityName = f.Opportunity?.OpportunityName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Status = f.Status,
                AssignedUserId = f.AssignedUserId,
                AssignedUserName = f.AssignedUser?.Name,
                Notes = f.Notes,
                CreatedDate = f.CreatedDate
            });

            return Ok(new { data = dtos, totalCount, page, pageSize });
        }

        /// <summary>
        /// Schedule a new follow-up
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var fu = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject,
                Status = model.Status,
                AssignedUserId = model.AssignedUserId ?? UserScopeHelper.GetUserId(User),
                Notes = model.Notes
            };

            string userId = UserScopeHelper.GetUserId(User)!;
            string userName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _followUpService.CreateFollowUpAsync(fu, userId, userName, ip);
            if (!success) return BadRequest(new { error });

            var dto = new FollowUpDto
            {
                FollowUpId = created!.FollowUpId,
                CustomerId = created.CustomerId,
                LeadId = created.LeadId,
                OpportunityId = created.OpportunityId,
                FollowUpDate = created.FollowUpDate,
                FollowUpType = created.FollowUpType,
                Subject = created.Subject,
                Status = created.Status,
                AssignedUserId = created.AssignedUserId,
                Notes = created.Notes,
                CreatedDate = created.CreatedDate
            };

            return CreatedAtAction(nameof(GetFollowUps), new { id = dto.FollowUpId }, dto);
        }
    }
}
