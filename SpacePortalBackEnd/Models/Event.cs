using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace SpacePortalBackEnd.Models
{
    [Table("Event")]
    public class Event
    {
        [Key]
        public long EventId { get; set; }
        [ForeignKey("EventType")]
        public long EventTypeId { get; set; }
        public string? ExternalId { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? OccuredAt { get; set; }
        public DateTime? EndAt { get; set; }
        public string? Description { get; set; }
        public string? Name { get; set; }
    }
}
