using System.Security.Claims;

namespace Text2Diagram_Backend.Authentication
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("User ID not found in claims.");
        }
    }
} 