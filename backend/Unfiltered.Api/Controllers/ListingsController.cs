using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Unfiltered.Api.Data;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Infrastructure;
using Unfiltered.Api.Models;
using Unfiltered.Api.Services;

namespace Unfiltered.Api.Controllers;

[Route("api/listings")]
public class ListingsController(AppDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<SearchResponse> Search([FromQuery] string? q)
    {
        q = (q ?? "").Trim();
        if (q.Length > 300) q = q[..300];

        var query = db.Listings.AsQueryable();
        var normalized = q.Length > 0 ? UrlNormalizer.Normalize(q) : null;
        if (normalized is not null)
            query = query.Where(l => l.Url == normalized.Url);
        else if (q.Length > 0)
            query = query.Where(l => l.Name.Contains(q) || (l.Platform != null && l.Platform.Contains(q)));

        var results = await Summaries(query)
            .OrderByDescending(s => s.ReviewCount)
            .ThenByDescending(s => s.Id)
            .Take(50)
            .ToListAsync();

        return new SearchResponse(q, normalized is not null, results);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, [FromQuery] string? sort, [FromQuery] bool verifiedOnly = false)
    {
        var summary = await Summaries(db.Listings.Where(l => l.Id == id)).SingleOrDefaultAsync();
        if (summary is null) return NotFoundError("We could not find that product or company.");

        var published = db.Reviews.Where(r => r.ListingId == id && r.Status == ReviewStatus.Published);

        var all = await published.Select(r => new { r.Rating, r.Tags }).ToListAsync();
        var distribution = new[] { 5, 4, 3, 2, 1 }.Select(stars => all.Count(r => r.Rating == stars)).ToArray();
        var topTags = all
            .SelectMany(r => r.Tags.SplitTags())
            .GroupBy(t => t)
            .Select(g => new TagCount(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ToList();

        var reviewQuery = published.Include(r => r.User).Include(r => r.Listing).AsQueryable();
        if (verifiedOnly) reviewQuery = reviewQuery.Where(r => r.Verified);
        reviewQuery = sort switch
        {
            "lowest" => reviewQuery.OrderBy(r => r.Rating).ThenByDescending(r => r.PublishedAt),
            "highest" => reviewQuery.OrderByDescending(r => r.Rating).ThenByDescending(r => r.PublishedAt),
            _ => reviewQuery.OrderByDescending(r => r.PublishedAt),
        };
        var reviews = (await reviewQuery.Take(200).ToListAsync()).Select(r => r.ToCard()).ToList();

        return Ok(new ListingDetailResponse(summary, distribution, topTags, reviews));
    }

    private static IQueryable<ListingSummaryDto> Summaries(IQueryable<Listing> listings) =>
        listings
            .Select(l => new
            {
                Listing = l,
                Count = l.Reviews.Count(r => r.Status == ReviewStatus.Published),
                WeightedSum = l.Reviews.Where(r => r.Status == ReviewStatus.Published).Sum(r => r.Rating * (r.Verified ? 1.0 : 0.5)),
                Weight = l.Reviews.Where(r => r.Status == ReviewStatus.Published).Sum(r => r.Verified ? 1.0 : 0.5),
                Verified = l.Reviews.Count(r => r.Status == ReviewStatus.Published && r.Verified),
                Rejected = l.Reviews.Count(r => r.Status == ReviewStatus.Published && r.RejectedElsewhere),
            })
            .Select(x => new ListingSummaryDto
            {
                Id = x.Listing.Id,
                Type = x.Listing.Type,
                Name = x.Listing.Name,
                Platform = x.Listing.Platform,
                Url = x.Listing.Url,
                ReviewCount = x.Count,
                Score = x.Weight > 0 ? (double?)Math.Round(x.WeightedSum / x.Weight, 1) : null,
                VerifiedCount = x.Verified,
                RejectedElsewhereCount = x.Rejected,
            });
}
