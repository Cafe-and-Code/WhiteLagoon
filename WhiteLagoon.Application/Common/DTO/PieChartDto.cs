namespace WhiteLagoon.Web.ViewModels
{
    public class PieChartDto
    {
        public required decimal[] Series { get; set; }
        public required string[] Labels { get; set; }
    }
}