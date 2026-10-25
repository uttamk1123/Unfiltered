using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Data;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Infrastructure;
using Unfiltered.Api.Models;
using Unfiltered.Api.Services;

namespace Unfiltered.Api.Controllers;

[Route("api/moderation")]
[Authorize(Roles = nameof(UserRole.Moderator))]
public class ModerationController(AppDbContext db, FileStore files) : ApiControllerBase
{
    [HttpGet("queue")]
    public async Task<ModerationQueueResponse> Queue()
    {
        var pending = await db.Reviews
            .Where(r => r.Status == ReviewStatus.Pending)
            .Include(r => r.Listing).Include(r => r.User)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();

        var flags = await db.Flags
            .Where(f => f.Status == FlagStatus.Open)
            .OrderBy(f => f.CreatedAt)
            .Select(f => new OpenFlagDto(f.Id, f.ReviewId, f.Review.Listing.Name, f.Review.Rating, f.Review.Title, f.Review.Body, f.Reason, f.CreatedAt))
            .ToListAsync();

        return new ModerationQueueResponse(
            pending.Select(r => new PendingReviewDto(
                r.Id, r.ListingId, r.Listing.Name, r.Listing.Type, r.Listing.Url, r.User.DisplayName, r.User.Email,
                r.Rating, r.Title, r.Body, r.Tags.SplitTags(), r.BoughtOn, r.PurchaseDate,
                r.RiskNotes.Split('\n', StringSplitOptions.RemoveEmptyEntries), r.ProofFile, r.RejectionFile, r.CreatedAt)).ToList(),
            flags);
    }

    [HttpGet("files/{name}")]
    public IActionResult GetFile(string name)
    {
        if (files.Open(name) is not { } file)
            return NotFoundError("File not found. It may have been deleted after the retention period.");
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(file.Path, file.ContentType);
    }

    [HttpPost("reviews/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, ApproveRequest req)
    {
        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.Status == ReviewStatus.Pending);
        if (review is null) return NotFoundError("That review is no longer waiting for moderation.");

        review.Verified = req.Verified && review.ProofFile is not null;
        review.Status = ReviewStatus.Published;
        review.PublishedAt = DateTime.UtcNow;
        Log(id, review.Verified ? "Published as a verified purchase" : "Published", null);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse($"Review #{id} published."));
    }

    [HttpPost("reviews/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, ReasonRequest req)
    {
        var reason = (req.Reason ?? "").Trim();
        if (reason.Length is 0 or > 1000) return Fail("Give the reviewer a reason when rejecting (up to 1,000 characters).");

        var review = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.Status == ReviewStatus.Pending);
        if (review is null) return NotFoundError("That review is no longer waiting for moderation.");

        review.Status = ReviewStatus.Rejected;
        Log(id, "Rejected", reason);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse($"Review #{id} rejected."));
    }

    [HttpPost("flags/{id:int}/dismiss")]
    public async Task<IActionResult> Dismiss(int id, ReasonRequest req)
    {
        var flag = await db.Flags.SingleOrDefaultAsync(f => f.Id == id && f.Status == FlagStatus.Open);
        if (flag is null) return NotFoundError("That report is already closed.");

        var note = (req.Reason ?? "").Trim();
        flag.Status = FlagStatus.Dismissed;
        Log(flag.ReviewId, "Report reviewed, review kept", note.Length > 0 ? note[..Math.Min(note.Length, 1000)] : "No rule was broken.");
        await db.SaveChangesAsync();
        return Ok(new MessageResponse("Report dismissed. The review stays up."));
    }

    [HttpPost("flags/{id:int}/uphold")]
    public async Task<IActionResult> Uphold(int id, ReasonRequest req)
    {
        var reason = (req.Reason ?? "").Trim();
        if (reason.Length is 0 or > 1000) return Fail("Removing a review needs a public reason (up to 1,000 characters).");

        var flag = await db.Flags.Include(f => f.Review).SingleOrDefaultAsync(f => f.Id == id && f.Status == FlagStatus.Open);
        if (flag is null) return NotFoundError("That report is already closed.");

        await db.Flags
            .Where(f => f.ReviewId == flag.ReviewId && f.Status == FlagStatus.Open)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.Status, FlagStatus.Upheld));
        flag.Review.Status = ReviewStatus.Removed;
        Log(flag.ReviewId, "Removed after a report", reason);
        await db.SaveChangesAsync();
        return Ok(new MessageResponse($"Review #{flag.ReviewId} removed."));
    }

    private void Log(int reviewId, string action, string? reason) =>
        db.ModerationLog.Add(new ModerationLogEntry { ReviewId = reviewId, ModeratorId = CurrentUserId, Action = action, Reason = reason });
}
