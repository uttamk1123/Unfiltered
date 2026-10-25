using System.Security.Claims;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Models;

namespace Unfiltered.Api.Infrastructure;

public static class Extensions
{
    public static int? UserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static bool IsModerator(this ClaimsPrincipal user) => user.IsInRole(nameof(UserRole.Moderator));

    public static string[] SplitTags(this string tags) =>
        tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Requires <see cref="Review.Listing"/> and <see cref="Review.User"/> to be loaded.</summary>
    public static ReviewCardDto ToCard(this Review r) => new(
        r.Id, r.ListingId, r.Listing.Name, r.User.DisplayName, r.Rating, r.Title, r.Body, r.Tags.SplitTags(),
        r.BoughtOn, r.PurchaseDate, r.Verified, r.RejectedElsewhere, r.Status, r.CreatedAt, r.PublishedAt);

    public static UserDto ToDto(this User u) => new(u.Id, u.Email, u.DisplayName, u.Role);
}
