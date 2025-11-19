using System.ComponentModel.DataAnnotations;
using Conquest.Models.Places;

namespace Conquest.Models.Activities
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
