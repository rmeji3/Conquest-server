using System.ComponentModel.DataAnnotations;
using Ping.Models.Places;

namespace Ping.Models.Activities
{
    public class ActivityKind
    {
        public int Id { get; set; }
        [MaxLength(200)]
        public required string Name { get; set; } // "Soccer", "Rock climbing"

        // nav
        public List<PlaceActivity> PlaceActivities { get; set; } = [];
    }

}

