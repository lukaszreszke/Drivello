using System.ComponentModel.DataAnnotations;

namespace Drivello.Models;

public class IssueDto
{
    [Required] public int UserId { get; set; }

    [Required] public int RentalId { get; set; }

    [Required] [StringLength(200)] public string Reason { get; set; }

    [Required] [StringLength(1000)] public string Description { get; set; }

    public bool IsDraft { get; set; }
}