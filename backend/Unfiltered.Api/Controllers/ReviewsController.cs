using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Data;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Infrastructure;
using Unfiltered.Api.Models;
using Unfiltered.Api.Services;

namespace Unfiltered.Api.Controllers;

[Route("api/reviews")]
public class ReviewsController(AppDbContext db, FileStore files) : ApiControllerBase
{
    private const int MaxReviewsPerDay = 5;

    [HttpGet("home")]
    public async Task<HomeResponse> Home()
    {
        var published = db.Reviews.Where(r => r.Status == ReviewStatus.Published);
        var recent = await published
            .Include(r => r.Listing).Include(r => r.User)
            .OrderByDescending(r => r.PublishedAt)
            .Take(10)
            .ToListAsync();
        return new HomeResponse(
            await published.CountAsync(),
            await published.CountAsync(r => r.RejectedElsewhere),
            recent.Select(r => r.ToCard()).ToList());
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<List<MyReviewDto>> Mine()
    {
        var userId = CurrentUserId;
        return await db.Reviews
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MyReviewDto(
                r.Id, r.Title, r.ListingId, r.Listing.Name, r.Status,
                r.Log.OrderByDescending(l => l.Id).Select(l => l.Reason).FirstOrDefault(),
                r.CreatedAt))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var review = await db.Reviews
            .Include(r => r.Listing).Include(r => r.User)
            .Include(r => r.Log.OrderBy(l => l.Id))
            .SingleOrDefaultAsync(r => r.Id == id);

        var isOwner = review is not null && review.UserId == User.UserId();
        var isMod = User.IsModerator();
        // Removed reviews stay visible as a tombstone with their moderation history, for transparency.
        if (review is null || (review.Status is ReviewStatus.Pending or ReviewStatus.Rejected && !isOwner && !isMod))
            return NotFoundError("We could not find that review.");

        var showBody = review.Status == ReviewStatus.Published || isOwner || isMod;
        var card = review.ToCard();
        if (!showBody) card = card with { Title = "Removed review", Body = "" };

        var log = review.Log.Select(l => new LogEntryDto(l.Action, l.Reason, l.CreatedAt)).ToList();
        return Ok(new ReviewDetailResponse(card, showBody, isOwner, log));
    }

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateReviewForm form, CancellationToken ct)
    {
        var userId = CurrentUserId;
        var since = DateTime.UtcNow.AddDays(-1);
        if (await db.Reviews.CountAsync(r => r.UserId == userId && r.CreatedAt > since, ct) >= MaxReviewsPerDay)
            return Fail($"You can post up to {MaxReviewsPerDay} reviews a day. Please try again tomorrow.", StatusCodes.Status429TooManyRequests);

        var title = (form.Title ?? "").Trim();
        var body = (form.Body ?? "").Trim();
        var tags = (form.Tags ?? []).Where(ReviewTags.Allowed.Contains).Distinct().ToArray();
        var rejectedElsewhere = form.RejectedElsewhere || form.Rejection is not null;

        if (form.Rating is not (>= 1 and <= 5)) return Fail("Pick a star rating from 1 to 5.");
        if (title.Length is < 5 or > 120) return Fail("The title must be 5 to 120 characters.");
        if (body.Length is < 50 or > 5000) return Fail("The review must be 50 to 5,000 characters. Describe what happened.");
        if (form.PurchaseDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))) return Fail("The purchase date cannot be in the future.");
        if (rejectedElsewhere && form.Rejection is null) return Fail("Attach a screenshot of the rejection to get the \"Rejected elsewhere\" badge, or untick that box.");
        if (!form.Agree) return Fail("Please confirm this review is your own honest experience.");
        foreach (var file in new[] { form.Proof, form.Rejection })
        {
            if (file is not null && FileStore.Validate(file) is { } fileError) return Fail(fileError);
        }

        Listing? listing;
        if (form.ListingId is int listingId)
        {
            listing = await db.Listings.FindAsync([listingId], ct);
            if (listing is null) return Fail("That product or company no longer exists. Start again from search.");
        }
        else
        {
            if (form.ListingType is null) return Fail("Choose whether you are reviewing a product, seller or service company.");
            UrlNormalizer.NormalizedUrl? normalized = null;
            listing = null;
            if (!string.IsNullOrWhiteSpace(form.ListingUrl))
            {
                normalized = UrlNormalizer.Normalize(form.ListingUrl);
                if (normalized is null) return Fail("That link does not look like a valid web address.");
                listing = await db.Listings.SingleOrDefaultAsync(l => l.Url == normalized.Url, ct);
            }
            if (listing is null)
            {
                var name = (form.ListingName ?? "").Trim();
                if (name.Length is < 2 or > 150) return Fail("Enter the product or company name (2 to 150 characters).");
                listing = new Listing { Type = form.ListingType.Value, Name = name, Platform = normalized?.Platform, Url = normalized?.Url };
                db.Listings.Add(listing);
            }
        }

        var screened = ContentScreener.Screen(title, body);
        var boughtOn = (form.BoughtOn ?? "").Trim();
        var review = new Review
        {
            Listing = listing,
            UserId = userId,
            Rating = form.Rating.Value,
            Title = screened.Title,
            Body = screened.Body,
            Tags = string.Join(',', tags),
            BoughtOn = boughtOn.Length > 0 ? boughtOn[..Math.Min(boughtOn.Length, 60)] : listing.Platform,
            PurchaseDate = form.PurchaseDate,
            RejectedElsewhere = rejectedElsewhere,
            RiskNotes = string.Join('\n', screened.Notes),
        };

        try
        {
            if (form.Proof is not null) review.ProofFile = await files.SaveAsync(form.Proof, ct);
            if (form.Rejection is not null) review.RejectionFile = await files.SaveAsync(form.Rejection, ct);
            db.Reviews.Add(review);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            files.Delete(review.ProofFile);
            files.Delete(review.RejectionFile);
            throw;
        }

        return CreatedAtAction(nameof(Get), new { id = review.Id },
            new CreatedResponse(review.Id, "Thanks. Your review is in the moderation queue and usually goes live within 24 hours."));
    }

    [HttpPost("{id:int}/flags")]
    [Authorize]
    public async Task<IActionResult> Flag(int id, ReasonRequest req)
    {
        var userId = CurrentUserId;
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.Status == ReviewStatus.Published);
        if (review is null) return NotFoundError("We could not find that review.");
        if (review.UserId == userId) return Fail("You can't report your own review.");

        var reason = (req.Reason ?? "").Trim();
        if (reason.Length is < 5 or > 1000) return Fail("Tell us in a few words (5 to 1,000 characters) why this review breaks the rules.");

        var alreadyOpen = await db.Flags.AnyAsync(f => f.ReviewId == id && f.UserId == userId && f.Status == FlagStatus.Open);
        if (!alreadyOpen)
        {
            db.Flags.Add(new Flag { ReviewId = id, UserId = userId, Reason = reason });
            await db.SaveChangesAsync();
        }
        return Ok(new MessageResponse("Thanks. A moderator will look at your report."));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId);
        if (review is null) return NotFoundError("We could not find that review.");
        if (review.Status != ReviewStatus.Removed)
        {
            review.Status = ReviewStatus.Removed;
            db.ModerationLog.Add(new ModerationLogEntry { ReviewId = id, Action = "Removed by its author" });
            await db.SaveChangesAsync();
        }
        return NoContent();
    }
}
