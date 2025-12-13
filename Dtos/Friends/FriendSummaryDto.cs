namespace Ping.Dtos.Friends
{
     public class FriendSummaryDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ? ProfileImageUrl { get; set; }
    }
}

