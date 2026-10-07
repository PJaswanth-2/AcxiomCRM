using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure Database Created with all tables, constraints, & indexes
            await context.Database.EnsureCreatedAsync();

            // 1. Roles
            string[] roles = { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Demo Users
            var adminEmail = "admin@acxiomcrm.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var managerEmail = "manager@acxiomcrm.com";
            var managerUser = await userManager.FindByEmailAsync(managerEmail);
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    Name = "Sarah Jenkins (Manager)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            var salesEmail = "sales@acxiomcrm.com";
            var salesUser = await userManager.FindByEmailAsync(salesEmail);
            if (salesUser == null)
            {
                salesUser = new ApplicationUser
                {
                    UserName = salesEmail,
                    Email = salesEmail,
                    Name = "Alex Rivera (Sales Exec)",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser, "Sales@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser, "SalesExecutive");
                }
            }

            // Fetch created user IDs
            var adminId = adminUser.Id;
            var managerId = managerUser.Id;
            var salesId = salesUser.Id;

            // 3. Customers (at least 10)
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer { CustomerCode = "CUST-1001", CustomerName = "Acme Global Solutions", Email = "contact@acmeglobal.com", Phone = "+1-555-0192", CompanyName = "Acme Corp", Address = "100 Innovation Way", City = "Austin", State = "TX", Status = "Active", CreatedBy = adminId, AssignedToUserId = salesId, Notes = "Enterprise enterprise client" },
                    new Customer { CustomerCode = "CUST-1002", CustomerName = "Apex Logistics", Email = "info@apexlogistics.com", Phone = "+1-555-0283", CompanyName = "Apex Logistics Inc.", Address = "45 Harbor Blvd", City = "Seattle", State = "WA", Status = "Active", CreatedBy = managerId, AssignedToUserId = salesId, Notes = "High priority freight customer" },
                    new Customer { CustomerCode = "CUST-1003", CustomerName = "Starlight Retail Group", Email = "purchasing@starlight.com", Phone = "+1-555-0374", CompanyName = "Starlight Retail", Address = "789 Market Street", City = "Chicago", State = "IL", Status = "Active", CreatedBy = salesId, AssignedToUserId = salesId, Notes = "National retail chain" },
                    new Customer { CustomerCode = "CUST-1004", CustomerName = "Nexus Cloud Technologies", Email = "billing@nexuscloud.io", Phone = "+1-555-0465", CompanyName = "Nexus Cloud", Address = "500 Silicon Ave", City = "San Jose", State = "CA", Status = "Active", CreatedBy = managerId, AssignedToUserId = managerId, Notes = "SaaS Infrastructure account" },
                    new Customer { CustomerCode = "CUST-1005", CustomerName = "Vanguard Financial Services", Email = "support@vanguardfin.com", Phone = "+1-555-0556", CompanyName = "Vanguard Fin", Address = "120 Wall St", City = "New York", State = "NY", Status = "Active", CreatedBy = adminId, AssignedToUserId = salesId, Notes = "FinTech compliance buyer" },
                    new Customer { CustomerCode = "CUST-1006", CustomerName = "OmniHealth Labs", Email = "procurement@omnihealth.org", Phone = "+1-555-0647", CompanyName = "OmniHealth", Address = "33 Medical Park Dr", City = "Boston", State = "MA", Status = "Active", CreatedBy = managerId, AssignedToUserId = managerId, Notes = "Healthcare software user" },
                    new Customer { CustomerCode = "CUST-1007", CustomerName = "BlueHorizon Energy", Email = "ops@bluehorizonenergy.com", Phone = "+1-555-0738", CompanyName = "BlueHorizon Energy", Address = "88 Solar Way", City = "Denver", State = "CO", Status = "Active", CreatedBy = salesId, AssignedToUserId = salesId, Notes = "Renewable energy client" },
                    new Customer { CustomerCode = "CUST-1008", CustomerName = "Quantum AI Robotics", Email = "dev@quantumrobotics.ai", Phone = "+1-555-0829", CompanyName = "Quantum AI", Address = "12 Cyber Park", City = "Atlanta", State = "GA", Status = "Active", CreatedBy = adminId, AssignedToUserId = salesId, Notes = "AI automation startup" },
                    new Customer { CustomerCode = "CUST-1009", CustomerName = "Pinnacle Media Group", Email = "ads@pinnaclemedia.com", Phone = "+1-555-0910", CompanyName = "Pinnacle Media", Address = "400 Sunset Blvd", City = "Los Angeles", State = "CA", Status = "Inactive", CreatedBy = managerId, AssignedToUserId = managerId, Notes = "Digital media partner" },
                    new Customer { CustomerCode = "CUST-1010", CustomerName = "Atlas Manufacturing Solutions", Email = "parts@atlasmfg.com", Phone = "+1-555-1001", CompanyName = "Atlas Mfg", Address = "90 Industrial Pkwy", City = "Detroit", State = "MI", Status = "Active", CreatedBy = salesId, AssignedToUserId = salesId, Notes = "Industrial machinery supplier" }
                };

                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // Fetch Customer IDs
            var customerList = await context.Customers.ToListAsync();

            // 4. Leads (at least 15)
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new Lead { LeadCode = "LEAD-2001", LeadName = "Marcus Vance", Email = "mvance@cybersec.io", Phone = "+1-555-2010", CompanyName = "Vance CyberSec", Source = "Website", Status = "New", Priority = "High", ExpectedValue = 45000, AssignedToUserId = salesId, Notes = "Inquired via homepage contact form." },
                    new Lead { LeadCode = "LEAD-2002", LeadName = "Elena Rostova", Email = "elena@biogen.com", Phone = "+1-555-2020", CompanyName = "BioGen Therapeutics", Source = "Referral", Status = "Contacted", Priority = "High", ExpectedValue = 85000, AssignedToUserId = salesId, Notes = "Referred by Starlight Retail Group." },
                    new Lead { LeadCode = "LEAD-2003", LeadName = "David Chen", Email = "dchen@innovate.co", Phone = "+1-555-2030", CompanyName = "Innovate Co", Source = "Cold Call", Status = "Qualified", Priority = "Medium", ExpectedValue = 30000, AssignedToUserId = salesId, Notes = "Qualified after initial discovery call." },
                    new Lead { LeadCode = "LEAD-2004", LeadName = "Sophia Martinez", Email = "smartinez@urbanbuild.com", Phone = "+1-555-2040", CompanyName = "Urban Build Group", Source = "Trade Show", Status = "Qualified", Priority = "High", ExpectedValue = 120000, AssignedToUserId = managerId, Notes = "Met at TechEx 2026." },
                    new Lead { LeadCode = "LEAD-2005", LeadName = "Robert Taylor", Email = "rtaylor@taylorlaw.com", Phone = "+1-555-2050", CompanyName = "Taylor & Partners", Source = "Campaign", Status = "Unqualified", Priority = "Low", ExpectedValue = 10000, AssignedToUserId = salesId, Notes = "Budget too small for enterprise tier." },
                    new Lead { LeadCode = "LEAD-2006", LeadName = "Amanda White", Email = "awhite@ecofood.org", Phone = "+1-555-2060", CompanyName = "EcoFood Distro", Source = "Social Media", Status = "Contacted", Priority = "Medium", ExpectedValue = 25000, AssignedToUserId = salesId, Notes = "LinkedIn message response." },
                    new Lead { LeadCode = "LEAD-2007", LeadName = "Kevin Patel", Email = "kpatel@cloudfront.net", Phone = "+1-555-2070", CompanyName = "CloudFront Networks", Source = "Website", Status = "Converted", Priority = "High", ExpectedValue = 95000, AssignedToUserId = salesId, ConvertedCustomerId = customerList[0].CustomerId, Notes = "Converted to customer Acme Global." },
                    new Lead { LeadCode = "LEAD-2008", LeadName = "Jessica Green", Email = "jgreen@greenh.com", Phone = "+1-555-2080", CompanyName = "Green House Inc", Source = "Referral", Status = "Lost", Priority = "Low", ExpectedValue = 15000, AssignedToUserId = salesId, Notes = "Selected competitor." },
                    new Lead { LeadCode = "LEAD-2009", LeadName = "Brian O'Connor", Email = "brian@fastfreight.com", Phone = "+1-555-2090", CompanyName = "Fast Freight", Source = "Cold Call", Status = "New", Priority = "Medium", ExpectedValue = 50000, AssignedToUserId = managerId, Notes = "Wants product demo next week." },
                    new Lead { LeadCode = "LEAD-2010", LeadName = "Catherine Bell", Email = "cbell@bellmedia.tv", Phone = "+1-555-2100", CompanyName = "Bell Media Network", Source = "Campaign", Status = "Contacted", Priority = "High", ExpectedValue = 75000, AssignedToUserId = salesId, Notes = "Email newsletter subscriber." },
                    new Lead { LeadCode = "LEAD-2011", LeadName = "Daniel Craig", Email = "dcraig@mi6tech.co.uk", Phone = "+1-555-2110", CompanyName = "MI6 Tech Ltd", Source = "Website", Status = "Qualified", Priority = "High", ExpectedValue = 110000, AssignedToUserId = salesId, Notes = "Needs security audit solution." },
                    new Lead { LeadCode = "LEAD-2012", LeadName = "Fiona Gallagher", Email = "fgallagher@southside.org", Phone = "+1-555-2120", CompanyName = "Southside Services", Source = "Social Media", Status = "New", Priority = "Low", ExpectedValue = 18000, AssignedToUserId = salesId, Notes = "Inquired via Twitter DM." },
                    new Lead { LeadCode = "LEAD-2013", LeadName = "George Harris", Email = "gharris@harrisholding.com", Phone = "+1-555-2130", CompanyName = "Harris Holdings", Source = "Trade Show", Status = "Contacted", Priority = "Medium", ExpectedValue = 65000, AssignedToUserId = managerId, Notes = "Exchanged business cards." },
                    new Lead { LeadCode = "LEAD-2014", LeadName = "Hannah Abbott", Email = "habbott@botanical.io", Phone = "+1-555-2140", CompanyName = "Botanical Labs", Source = "Referral", Status = "Qualified", Priority = "Medium", ExpectedValue = 35000, AssignedToUserId = salesId, Notes = "Ready for contract proposal." },
                    new Lead { LeadCode = "LEAD-2015", LeadName = "Ian Malcolm", Email = "imalcolm@chaos.edu", Phone = "+1-555-2150", CompanyName = "Chaos Systems", Source = "Website", Status = "New", Priority = "High", ExpectedValue = 150000, AssignedToUserId = salesId, Notes = "Enterprise platform request." }
                };

                await context.Leads.AddRangeAsync(leads);
                await context.SaveChangesAsync();
            }

            var leadList = await context.Leads.ToListAsync();

            // 5. Opportunities (at least 10)
            if (!await context.Opportunities.AnyAsync())
            {
                var today = DateTime.Today;
                var opportunities = new List<Opportunity>
                {
                    new Opportunity { OpportunityName = "Acme Global - Enterprise CRM Upgrade", CustomerId = customerList[0].CustomerId, Amount = 95000, Stage = "Negotiation", Probability = 80, ExpectedCloseDate = today.AddDays(15), Status = "Open", AssignedToUserId = salesId, Source = "Existing Client", Notes = "Final legal review of SLA." },
                    new Opportunity { OpportunityName = "Apex Freight Operations Portal", CustomerId = customerList[1].CustomerId, Amount = 60000, Stage = "Proposal", Probability = 50, ExpectedCloseDate = today.AddDays(30), Status = "Open", AssignedToUserId = salesId, Source = "Referral", Notes = "RFP proposal submitted." },
                    new Opportunity { OpportunityName = "Starlight POS System Integration", CustomerId = customerList[2].CustomerId, Amount = 120000, Stage = "Won", Probability = 100, ExpectedCloseDate = today.AddDays(-5), Status = "Won", AssignedToUserId = salesId, Source = "Website", Notes = "Contract signed and deposit received." },
                    new Opportunity { OpportunityName = "Nexus Cloud Multi-Tenant Add-on", CustomerId = customerList[3].CustomerId, Amount = 45000, Stage = "Qualification", Probability = 20, ExpectedCloseDate = today.AddDays(45), Status = "Open", AssignedToUserId = managerId, Source = "Inbound", Notes = "Discovery session scheduled." },
                    new Opportunity { OpportunityName = "Vanguard Compliance Suite", CustomerId = customerList[4].CustomerId, Amount = 180000, Stage = "Negotiation", Probability = 75, ExpectedCloseDate = today.AddDays(10), Status = "Open", AssignedToUserId = salesId, Source = "Campaign", Notes = "Pricing review with CFO." },
                    new Opportunity { OpportunityName = "OmniHealth EMR Connector", CustomerId = customerList[5].CustomerId, Amount = 70000, Stage = "Proposal", Probability = 60, ExpectedCloseDate = today.AddDays(20), Status = "Open", AssignedToUserId = managerId, Source = "Trade Show", Notes = "Technical demonstration completed." },
                    new Opportunity { OpportunityName = "BlueHorizon Solar Monitoring", CustomerId = customerList[6].CustomerId, Amount = 35000, Stage = "Lost", Probability = 0, ExpectedCloseDate = today.AddDays(-10), Status = "Lost", AssignedToUserId = salesId, Source = "Cold Call", Notes = "Project postponed indefinitely." },
                    new Opportunity { OpportunityName = "Quantum AI Automation Hub", CustomerId = customerList[7].CustomerId, Amount = 210000, Stage = "Qualification", Probability = 30, ExpectedCloseDate = today.AddDays(60), Status = "Open", AssignedToUserId = salesId, Source = "Referral", Notes = "Initial requirements gathering." },
                    new Opportunity { OpportunityName = "Atlas Manufacturing ERP Link", CustomerId = customerList[9].CustomerId, Amount = 115000, Stage = "Proposal", Probability = 50, ExpectedCloseDate = today.AddDays(25), Status = "Open", AssignedToUserId = salesId, Source = "Website", Notes = "Drafting scope of work." },
                    new Opportunity { OpportunityName = "Innovate Co - Cloud CRM Pilot", LeadId = leadList[2].LeadId, Amount = 28000, Stage = "Qualification", Probability = 25, ExpectedCloseDate = today.AddDays(40), Status = "Open", AssignedToUserId = salesId, Source = "Cold Call", Notes = "Proof of concept evaluation." }
                };

                await context.Opportunities.AddRangeAsync(opportunities);
                await context.SaveChangesAsync();
            }

            var oppList = await context.Opportunities.ToListAsync();

            // 6. Follow-Ups (at least 10)
            if (!await context.FollowUps.AnyAsync())
            {
                var today = DateTime.Today;
                var followUps = new List<FollowUp>
                {
                    new FollowUp { CustomerId = customerList[0].CustomerId, OpportunityId = oppList[0].OpportunityId, FollowUpDate = today.AddDays(2), FollowUpType = "Meeting", Subject = "Contract SLA Final Review", Status = "Planned", AssignedUserId = salesId, Notes = "Review redlines with Acme legal team." },
                    new FollowUp { CustomerId = customerList[1].CustomerId, OpportunityId = oppList[1].OpportunityId, FollowUpDate = today.AddDays(5), FollowUpType = "Call", Subject = "Follow up on RFP Submission", Status = "Planned", AssignedUserId = salesId, Notes = "Check if evaluation board has questions." },
                    new FollowUp { LeadId = leadList[0].LeadId, FollowUpDate = today.AddDays(1), FollowUpType = "Email", Subject = "Send CyberSec Product Brochure", Status = "Planned", AssignedUserId = salesId, Notes = "Include pricing matrix." },
                    new FollowUp { LeadId = leadList[1].LeadId, FollowUpDate = today.AddDays(3), FollowUpType = "Call", Subject = "Discovery Call with Elena", Status = "Planned", AssignedUserId = salesId, Notes = "Understand BioGen pipeline requirements." },
                    new FollowUp { CustomerId = customerList[4].CustomerId, OpportunityId = oppList[4].OpportunityId, FollowUpDate = today.AddDays(-2), FollowUpType = "Call", Subject = "Vanguard CFO Pricing Sync", Status = "Missed", AssignedUserId = salesId, Notes = "Reschedule ASAP - client was in meeting." },
                    new FollowUp { CustomerId = customerList[2].CustomerId, OpportunityId = oppList[2].OpportunityId, FollowUpDate = today.AddDays(-4), FollowUpType = "Meeting", Subject = "Project Kickoff Meeting", Status = "Completed", AssignedUserId = salesId, Notes = "Introduced implementation team." },
                    new FollowUp { LeadId = leadList[3].LeadId, FollowUpDate = today.AddDays(4), FollowUpType = "Meeting", Subject = "On-site Demo for Urban Build", Status = "Planned", AssignedUserId = managerId, Notes = "Prepare slide deck and live sandbox." },
                    new FollowUp { LeadId = leadList[8].LeadId, FollowUpDate = today, FollowUpType = "Call", Subject = "Schedule Fast Freight Product Demo", Status = "Planned", AssignedUserId = managerId, Notes = "Call Brian at 2:00 PM." },
                    new FollowUp { CustomerId = customerList[5].CustomerId, OpportunityId = oppList[5].OpportunityId, FollowUpDate = today.AddDays(7), FollowUpType = "Task", Subject = "Send Security Compliance Docs", Status = "Planned", AssignedUserId = managerId, Notes = "Attach ISO 27001 certificate." },
                    new FollowUp { CustomerId = customerList[7].CustomerId, OpportunityId = oppList[7].OpportunityId, FollowUpDate = today.AddDays(10), FollowUpType = "Email", Subject = "Send Architecture Blueprint", Status = "Planned", AssignedUserId = salesId, Notes = "Coordinate with engineering team." }
                };

                await context.FollowUps.AddRangeAsync(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Activities (at least 10)
            if (!await context.Activities.AnyAsync())
            {
                var today = DateTime.Today;
                var activities = new List<Activity>
                {
                    new Activity { ActivityType = "Call", Subject = "Initial Discovery Call", Description = "Discussed CRM needs with Acme leadership.", ActivityDate = today.AddDays(-15), CustomerId = customerList[0].CustomerId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Meeting", Subject = "RFP Defense Meeting", Description = "Presented proposed solution to Apex board.", ActivityDate = today.AddDays(-10), CustomerId = customerList[1].CustomerId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Email", Subject = "Sent Contract Document", Description = "Emailed final contract agreement for signature.", ActivityDate = today.AddDays(-5), CustomerId = customerList[2].CustomerId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Call", Subject = "Cold Outreach Call", Description = "Spoke with Marcus regarding cybersecurity suite.", ActivityDate = today.AddDays(-8), LeadId = leadList[0].LeadId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Meeting", Subject = "Executive Briefing", Description = "High-level overview meeting with Vanguard executives.", ActivityDate = today.AddDays(-3), CustomerId = customerList[4].CustomerId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Task", Subject = "Prepared Custom Proposal", Description = "Built custom pricing quote for OmniHealth.", ActivityDate = today.AddDays(-6), CustomerId = customerList[5].CustomerId, AssignedToUserId = managerId, Status = "Completed" },
                    new Activity { ActivityType = "Call", Subject = "Inbound Inquiry Response", Description = "Answered technical questions for Quantum AI.", ActivityDate = today.AddDays(-4), CustomerId = customerList[7].CustomerId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Email", Subject = "Follow-up Note", Description = "Sent thank-you email following trade show.", ActivityDate = today.AddDays(-12), LeadId = leadList[3].LeadId, AssignedToUserId = managerId, Status = "Completed" },
                    new Activity { ActivityType = "Meeting", Subject = "Technical Architecture Review", Description = "Reviewed database integration specs with Atlas Mfg.", ActivityDate = today.AddDays(-2), CustomerId = customerList[9].CustomerId, AssignedToUserId = salesId, Status = "Completed" },
                    new Activity { ActivityType = "Task", Subject = "Lead Qualification Check", Description = "Verified company size and budget for Innovate Co.", ActivityDate = today.AddDays(-7), LeadId = leadList[2].LeadId, AssignedToUserId = salesId, Status = "Completed" }
                };

                await context.Activities.AddRangeAsync(activities);
                await context.SaveChangesAsync();
            }

            // 8. Initial Audit Logs
            if (!await context.AuditLogs.AnyAsync())
            {
                var auditLogs = new List<AuditLog>
                {
                    new AuditLog { UserId = adminId, UserName = adminEmail, Action = "System Seed", EntityName = "System", RecordId = "0", OldValue = null, NewValue = "Initialized Database", CreatedDate = DateTime.UtcNow, IpAddress = "127.0.0.1", Result = "Success", Details = "AcxiomCRM initial database seed executed successfully." },
                    new AuditLog { UserId = adminId, UserName = adminEmail, Action = "User Creation", EntityName = "User", RecordId = managerId, OldValue = null, NewValue = managerEmail, CreatedDate = DateTime.UtcNow, IpAddress = "127.0.0.1", Result = "Success", Details = "Created Manager user account." },
                    new AuditLog { UserId = adminId, UserName = adminEmail, Action = "User Creation", EntityName = "User", RecordId = salesId, OldValue = null, NewValue = salesEmail, CreatedDate = DateTime.UtcNow, IpAddress = "127.0.0.1", Result = "Success", Details = "Created Sales Executive user account." }
                };

                await context.AuditLogs.AddRangeAsync(auditLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}
