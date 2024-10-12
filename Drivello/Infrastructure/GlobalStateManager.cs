namespace Drivello.Infrastructure;

public sealed class GlobalStateManager
{
    internal GlobalStateManager() {}
    public int TotalActiveRentals { get; set; }
    public decimal TotalRevenue { get; set; }
}