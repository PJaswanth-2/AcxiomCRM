namespace AcxiomCRM.DTOs
{
    public class PipelineReportDto
    {
        public string Stage { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }
}
