using EventBookingSecure.Data;
using EventBookingSecure.Models;
using EventBookingSecure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EventBookingSecure.Controllers;

public class EventController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUrlValidator _urlValidator;

    public EventController(ApplicationDbContext context, IUrlValidator urlValidator)
    {
        _context = context;
        _urlValidator = urlValidator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? term)
    {
        var events = await _context.GetUpcomingEventsAsync(term);

        foreach (var evt in events)
        {
            if (!_urlValidator.IsSafe(evt.ImageUrl))
            {
                evt.ImageUrl = null;
            }
        }

        ViewData["SearchTerm"] = term ?? string.Empty;
        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var evt = await _context.Events
            .Include(e => e.Organizer)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt is null)
        {
            return NotFound();
        }

        if (!_urlValidator.IsSafe(evt.ImageUrl))
        {
            evt.ImageUrl = null;
        }

        return View(evt);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int eventId, int seats)
    {
        if (seats <= 0)
        {
            TempData["Error"] = "Please choose at least one seat.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Unable to determine the current user.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _context.TryBookTicketsAsync(eventId, userId, seats);

        TempData[result.Success ? "Message" : "Error"] = result.Success
            ? "Tickets booked successfully."
            : result.ErrorMessage;

        return RedirectToAction(nameof(Details), new { id = eventId });
    }

    [Authorize]
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var bookings = await _context.Bookings
            .Include(b => b.Event)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .AsNoTracking()
            .ToListAsync();

        foreach (var booking in bookings)
        {
            if (booking.Event is not null && !_urlValidator.IsSafe(booking.Event.ImageUrl))
            {
                booking.Event.ImageUrl = null;
            }
        }

        return View(bookings);
    }
}
