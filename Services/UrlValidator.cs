using System;
using Microsoft.AspNetCore.Http;

namespace EventBookingSecure.Services;

public class UrlValidator : IUrlValidator
{
    public bool IsLocalUrl(string? url, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!url.StartsWith('/'))
        {
            return false;
        }

        if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        return !url.Contains("://", StringComparison.Ordinal);
    }

    public string GetSafeReturnUrl(string? returnUrl, HttpContext context, string defaultUrl = "/")
    {
        return IsLocalUrl(returnUrl, context) ? returnUrl! : defaultUrl;
    }

    public bool IsSafe(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return true;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (parsed.Scheme is not ("http" or "https"))
        {
            return false;
        }

        return string.IsNullOrEmpty(parsed.UserInfo);
    }
}
