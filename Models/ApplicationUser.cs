using Microsoft.AspNetCore.Identity;

namespace EventBookingSecure.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FirstName { get; set; }

        [PersonalData]
        public string? LastName { get; set; }

        [PersonalData]
        public DateTime? DateOfBirth { get; set; }

        public ICollection<Event>? OrganizedEvents { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
    }
}