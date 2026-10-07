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
    [Route("api/opportunities")]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesApiController(IOpportunityService opportunityService)
        {
            _opportunityService = opportunityService;
        }

        /// <summary>
        /// Get opportunities list
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            string? scope = UserScopeHelper.IsSalesExecutive(User) ? UserScopeHelper.GetUserId(User) : null;
            var (items, totalCount) = await _opportunityService.GetOpportunitiesAsync(search, stage, status, null, scope, page, pageSize);

            var dtos = items.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedToUserId = o.AssignedToUserId,
                AssignedToName = o.AssignedTo?.Name,
                Source = o.Source,
                Notes = o.Notes
            });

            return Ok(new { data = dtos, totalCount, page, pageSize });
        }

        /// <summary>
        /// Create opportunity
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = model.Status,
                AssignedToUserId = model.AssignedToUserId ?? UserScopeHelper.GetUserId(User),
                Source = model.Source,
                Notes = model.Notes
            };

            string userId = UserScopeHelper.GetUserId(User)!;
            string userName = UserScopeHelper.GetUserName(User)!;
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, error, created) = await _opportunityService.CreateOpportunityAsync(opp, userId, userName, ip);
            if (!success) return BadRequest(new { error });

            var dto = new OpportunityDto
            {
                OpportunityId = created!.OpportunityId,
                OpportunityName = created.OpportunityName,
                CustomerId = created.CustomerId,
                LeadId = created.LeadId,
                Amount = created.Amount,
                Stage = created.Stage,
                Probability = created.Probability,
                ExpectedCloseDate = created.ExpectedCloseDate,
                Status = created.Status,
                CreatedDate = created.CreatedDate,
                AssignedToUserId = created.AssignedToUserId,
                Source = created.Source,
                Notes = created.Notes
            };

            return CreatedAtAction(nameof(GetOpportunities), new { id = dto.OpportunityId }, dto);
        }
    }
}
