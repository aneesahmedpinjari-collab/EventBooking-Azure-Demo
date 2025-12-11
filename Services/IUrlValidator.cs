using Microsoft.AspNetCore.Http;

namespace EventBookingSecure.Services;

public interface IUrlValidator
{
    bool IsLocalUrl(string? url, HttpContext context);
    string GetSafeReturnUrl(string? returnUrl, HttpContext context, string defaultUrl = "/");
    bool IsSafe(string? url);
}
