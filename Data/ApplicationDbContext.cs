using System;
using System.Linq;
using System.Threading.Tasks;
using EventBookingSecure.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventBookingSecure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Booking> Bookings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Event>()
            .HasOne(e => e.Organizer)
            .WithMany(u => u.OrganizedEvents)
            .HasForeignKey(e => e.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Event)
            .WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public async Task<List<Event>> GetUpcomingEventsAsync(string? term)
    {
        var query = Events
            .AsNoTracking()
            .Where(e => e.EventDate >= DateTime.Today);

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.Title, $"%{term}%") ||
                EF.Functions.Like(e.Description, $"%{term}%") ||
                EF.Functions.Like(e.Location, $"%{term}%"));
        }

        return await query
            .OrderBy(e => e.EventDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string ErrorMessage)> TryBookTicketsAsync(int eventId, string userId, int seats)
    {
        if (seats <= 0)
        {
            return (false, "Please select at least one seat.");
        }

        var evt = await Events.FirstOrDefaultAsync(e => e.Id == eventId);
        if (evt is null)
        {
            return (false, "The selected event could not be found.");
        }

        if (evt.AvailableSeats < seats)
        {
            return (false, "Not enough seats remaining for this event.");
        }

        evt.AvailableSeats -= seats;

        var booking = new Booking
        {
            EventId = evt.Id,
            UserId = userId,
            NumberOfSeats = seats,
            BookingDate = DateTime.UtcNow,
            TotalAmount = evt.Price * seats,
            Status = BookingStatus.Confirmed
        };

        Bookings.Add(booking);
        await SaveChangesAsync();
        return (true, string.Empty);
    }
}
