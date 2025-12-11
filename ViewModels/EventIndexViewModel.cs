using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventBookingSecure.Models.ViewModels;

public class EventIndexViewModel
{
    public IReadOnlyCollection<EventCardViewModel> Events { get; init; }
        = new List<EventCardViewModel>();

    public string? SearchTerm { get; init; }
        = string.Empty;

}

public class EventCardViewModel
{
    public Guid Id { get; init; }
        = Guid.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Location { get; init; } = string.Empty;

    public DateTimeOffset StartDate { get; init; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset EndDate { get; init; }
        = DateTimeOffset.UtcNow;

    public int TicketsRemaining { get; init; }
        = 0;

    public string? ImageUrl { get; init; }
        = null;
}

public class BookingRequestViewModel
{
    [Required]
    public Guid EventId { get; set; }
        = Guid.Empty;

    [Range(1, 10)]
    public int Tickets { get; set; }
        = 1;
}
