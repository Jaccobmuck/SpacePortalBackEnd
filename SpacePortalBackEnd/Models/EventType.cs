using System.ComponentModel.DataAnnotations.Schema;

namespace SpacePortalBackEnd.Models
{
    [Table("EventType")]
    public class EventType
    {
        public long EventTypeId { get; set; }
        public string? Description { get; set; }
        public ICollection<Event>? Events { get; set; }
    }
}
