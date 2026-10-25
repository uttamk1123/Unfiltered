using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Models;

namespace Unfiltered.Api.Data;

/// <summary>Adds demo accounts and sample reviews to an empty database (Development only).</summary>
public static class DemoSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> hasher)
    {
        if (await db.Users.AnyAsync()) return;

        User NewUser(string email, string password, string name, UserRole role)
        {
            var u = new User { Email = email, DisplayName = name, Role = role };
            u.PasswordHash = hasher.HashPassword(u, password);
            return u;
        }

        var mod = NewUser("mod@unfiltered.local", "moderator123", "Unfiltered Team", UserRole.Moderator);
        var priya = NewUser("priya@unfiltered.local", "buyer12345", "Priya S", UserRole.User);
        var rahul = NewUser("rahul@unfiltered.local", "buyer12345", "Rahul_K", UserRole.User);

        var earbuds = new Listing { Type = ListingType.Product, Name = "Wireless Earbuds X200", Platform = "Amazon", Url = "https://www.amazon.in/dp/B0EXAMPLE1" };
        var courier = new Listing { Type = ListingType.Service, Name = "QuickShip Courier" };
        var grocery = new Listing { Type = ListingType.Service, Name = "Instant Grocery App" };

        Review Published(Listing listing, User user, int rating, string title, string body, string tags, string boughtOn, bool verified, bool rejectedElsewhere) => new()
        {
            Listing = listing,
            User = user,
            Rating = rating,
            Title = title,
            Body = body,
            Tags = tags,
            BoughtOn = boughtOn,
            Verified = verified,
            RejectedElsewhere = rejectedElsewhere,
            Status = ReviewStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Log = [new ModerationLogEntry { Moderator = mod, Action = verified ? "Published as a verified purchase" : "Published" }],
        };

        db.Reviews.AddRange(
            Published(earbuds, priya, 2, "Left earbud died after 3 weeks, replacement refused",
                "The left earbud stopped charging after 21 days. I raised a return within the window, but the seller said \"physical damage\" without inspecting it. My review mentioning the refused replacement was rejected by the marketplace for \"seller feedback content\".",
                "quality,refund", "Amazon", verified: true, rejectedElsewhere: true),
            Published(earbuds, rahul, 4, "Good sound for the price",
                "Sound and battery are good for the price. Bass is strong, calls are average outdoors. Case feels cheap but has survived two months in my bag without problems.",
                "quality", "Amazon", verified: true, rejectedElsewhere: false),
            Published(courier, priya, 1, "Marked delivered, never arrived",
                "Tracking showed \"delivered\" at 2 pm while I was home all day. Support closed my complaint twice without calling back. It took 11 days and a consumer forum complaint to get a refund. The app only allows a private star rating.",
                "delivery,customer-care", "QuickShip", verified: true, rejectedElsewhere: false),
            Published(grocery, rahul, 2, "Prices higher in cart than on the listing",
                "Three times this month the price at checkout was higher than on the product page, by 10 to 25 rupees per item. The app has no public reviews, only a thumbs up or down after delivery.",
                "pricing", "Instant Grocery App", verified: false, rejectedElsewhere: false));

        await db.SaveChangesAsync();
    }
}
