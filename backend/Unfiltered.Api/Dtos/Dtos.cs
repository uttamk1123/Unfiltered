using Unfiltered.Api.Models;

namespace Unfiltered.Api.Dtos;

public record ErrorResponse(string Error);

public record MessageResponse(string Message);

// ---------- Auth ----------

public record UserDto(int Id, string Email, string DisplayName, UserRole Role);

public record MeResponse(UserDto? User);

public record RegisterRequest(string? Email, string? Password, string? DisplayName);

public record LoginRequest(string? Email, string? Password);

// ---------- Listings ----------

public record ListingSummaryDto
{
    public int Id { get; init; }
    public ListingType Type { get; init; }
    public string Name { get; init; } = "";
    public string? Platform { get; init; }
    public string? Url { get; init; }
    public int ReviewCount { get; init; }
    /// <summary>Weighted average: verified purchases count twice as much as unverified ones.</summary>
    public double? Score { get; init; }
    public int VerifiedCount { get; init; }
    public int RejectedElsewhereCount { get; init; }
}

public record SearchResponse(string Query, bool IsUrl, List<ListingSummaryDto> Results);

public record TagCount(string Tag, int Count);

public record ListingDetailResponse(ListingSummaryDto Listing, int[] Distribution, List<TagCount> TopTags, List<ReviewCardDto> Reviews);

// ---------- Reviews ----------

public record ReviewCardDto(
    int Id,
    int ListingId,
    string ListingName,
    string DisplayName,
    int Rating,
    string Title,
    string Body,
    string[] Tags,
    string? BoughtOn,
    DateOnly? PurchaseDate,
    bool Verified,
    bool RejectedElsewhere,
    ReviewStatus Status,
    DateTime CreatedAt,
    DateTime? PublishedAt);

public record LogEntryDto(string Action, string? Reason, DateTime CreatedAt);

public record ReviewDetailResponse(ReviewCardDto Review, bool ShowBody, bool IsOwner, List<LogEntryDto> Log);

public record HomeResponse(int PublishedCount, int RejectedElsewhereCount, List<ReviewCardDto> Recent);

public record MyReviewDto(int Id, string Title, int ListingId, string ListingName, ReviewStatus Status, string? LastReason, DateTime CreatedAt);

public record CreatedResponse(int Id, string Message);

public class CreateReviewForm
{
    public int? ListingId { get; set; }
    public ListingType? ListingType { get; set; }
    public string? ListingUrl { get; set; }
    public string? ListingName { get; set; }
    public int? Rating { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public List<string>? Tags { get; set; }
    public string? BoughtOn { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public bool RejectedElsewhere { get; set; }
    public bool Agree { get; set; }
    public IFormFile? Proof { get; set; }
    public IFormFile? Rejection { get; set; }
}

public record ReasonRequest(string? Reason);

// ---------- Moderation ----------

public record ApproveRequest(bool Verified);

public record PendingReviewDto(
    int Id,
    int ListingId,
    string ListingName,
    ListingType ListingType,
    string? ListingUrl,
    string DisplayName,
    string Email,
    int Rating,
    string Title,
    string Body,
    string[] Tags,
    string? BoughtOn,
    DateOnly? PurchaseDate,
    string[] RiskNotes,
    string? ProofFile,
    string? RejectionFile,
    DateTime CreatedAt);

public record OpenFlagDto(int Id, int ReviewId, string ListingName, int Rating, string Title, string Body, string Reason, DateTime CreatedAt);

public record ModerationQueueResponse(List<PendingReviewDto> Pending, List<OpenFlagDto> Flags);
