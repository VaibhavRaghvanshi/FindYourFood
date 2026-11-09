using System.Security.Claims;
using FindYourFood.Organization.Domain;

namespace FindYourFood.Organization.Api.Authentication;

internal static class StaffPrincipal
{
    public static StaffActor? GetStaffActor(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out var id) && id != Guid.Empty
            ? new StaffActor(id, principal.HasClaim("account_type", "owner"))
            : null;
}
