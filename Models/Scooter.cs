namespace Drivello.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public enum Range
{
    Short,
    Medium,
    Long
}

public class Scooter
{
    public int Id { get; set; }
    public string Status { get; set; }
    public List<Rental> Rentals { get; set; } = new List<Rental>();
    public List<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public DateTime LastMaintenanceDate { get; set; }
    public bool IsUnderMaintenance { get; set; }
    public decimal BasePricePerMinute { get; set; }
    public Range Range { get; set; }
}