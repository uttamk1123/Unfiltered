namespace Unfiltered.Api.Models;

public enum UserRole { User, Moderator }

public enum ListingType { Product, Seller, Service }

public enum ReviewStatus { Pending, Published, Rejected, Removed }

public enum FlagStatus { Open, Dismissed, Upheld }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Review> Reviews { get; set; } = [];
}

/// <summary>A product, seller or service company that people review.</summary>
public class Listing
{
    public int Id { get; set; }
    public ListingType Type { get; set; }
    public string Name { get; set; } = "";
    public string? Platform { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Review> Reviews { get; set; } = [];
}

public class Review
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    /// <summary>Comma-separated values from <see cref="ReviewTags.Allowed"/>.</summary>
    public string Tags { get; set; } = "";
    public string? BoughtOn { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public bool Verified { get; set; }
    public bool RejectedElsewhere { get; set; }
    public string? ProofFile { get; set; }
    public string? RejectionFile { get; set; }
    /// <summary>Newline-separated notes from the automatic pre-screen, shown only to moderators.</summary>
    public string RiskNotes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    public Listing Listing { get; set; } = null!;
    public User User { get; set; } = null!;
    public List<Flag> Flags { get; set; } = [];
    public List<ModerationLogEntry> Log { get; set; } = [];
}

public class Flag
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public string Reason { get; set; } = "";
    public FlagStatus Status { get; set; } = FlagStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Review Review { get; set; } = null!;
    public User User { get; set; } = null!;
}

/// <summary>Public history of moderation decisions on a review. The moderator is never shown publicly.</summary>
public class ModerationLogEntry
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public int? ModeratorId { get; set; }
    public string Action { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Review Review { get; set; } = null!;
    public User? Moderator { get; set; }
}

public static class ReviewTags
{
    public static readonly string[] Allowed = ["delivery", "refund", "quality", "customer-care", "fraud", "pricing", "other"];
}
