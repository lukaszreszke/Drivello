using System;

namespace Drivello.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public User User { get; set; }
        public Rental Rental { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public Statuses Status { get; set; }
        public DateTime? CompletionDate { get; set; }
        public CompletionModes? CompletionMode { get; set; }
        public DateTime? ChangedAt { get; set; }

        public enum Statuses
        {
            Draft,
            New,
            Escalated,
            Refunded,
            Resolved,
            Closed
        }

        public enum CompletionModes
        {
            Automatic,
            Manual
        }
    }
}