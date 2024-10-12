namespace Drivello.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int ViolationCount { get; set; }
    public bool IsSuspended { get; set; }
    public string Membership { get; set; }
    public List<Rental> Rentals { get; set; } = new List<Rental>();
    public DateTime LastRentalDate { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}