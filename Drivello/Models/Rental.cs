namespace Drivello.Models;

public class Rental
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public int ScooterId { get; set; }
    public Scooter Scooter { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}