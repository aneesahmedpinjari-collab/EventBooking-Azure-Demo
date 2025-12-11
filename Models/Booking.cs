using System.ComponentModel.DataAnnotations;

namespace EventBookingSecure.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 100)]
        public int NumberOfSeats { get; set; }

        public DateTime BookingDate { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        public BookingStatus Status { get; set; }
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}
