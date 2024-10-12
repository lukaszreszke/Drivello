namespace Drivello.Models;

public class MaintenanceRecord
{
    public int Id { get; set; }
    public int ScooterId { get; set; }
    public Scooter Scooter { get; set; }
    public DateTime MaintenanceDate { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
}