using System.ComponentModel.DataAnnotations;

namespace EventBookingSecure.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000)]
        public int Capacity { get; set; }

        public int AvailableSeats { get; set; }

        [Required]
        [Range(0, 10000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public string OrganizerId { get; set; } = string.Empty;
        public ApplicationUser? Organizer { get; set; }

        public ICollection<Booking>? Bookings { get; set; }
    }
}
