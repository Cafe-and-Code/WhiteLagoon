namespace WhiteLagoon.Web.ViewModel;

public class RadialBarChartVM
{
    public decimal TotalCount { get; set; }
    public decimal CountInCurrentMonth { get; set; }
    public bool HasRatioIncreased { get; set; }
    public required int[] Series { get; set; }
}
