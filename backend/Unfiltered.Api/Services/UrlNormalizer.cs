using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Unfiltered.Api.Services;

/// <summary>
/// Turns a pasted product or company link into a canonical URL so the same product
/// is not added twice (e.g. Amazon links with tracking parameters).
/// </summary>
public static partial class UrlNormalizer
{
    public record NormalizedUrl(string Url, string Platform);

    private static readonly Dictionary<string, string> Platforms = new()
    {
        ["amazon"] = "Amazon",
        ["flipkart"] = "Flipkart",
        ["myntra"] = "Myntra",
        ["meesho"] = "Meesho",
        ["ajio"] = "AJIO",
        ["nykaa"] = "Nykaa",
        ["bigbasket"] = "BigBasket",
        ["blinkit"] = "Blinkit",
        ["zepto"] = "Zepto",
        ["swiggy"] = "Swiggy",
        ["zomato"] = "Zomato",
        ["jiomart"] = "JioMart",
        ["tatacliq"] = "Tata CLiQ",
        ["snapdeal"] = "Snapdeal",
        ["ebay"] = "eBay",
        ["walmart"] = "Walmart",
        ["etsy"] = "Etsy",
    };

    [GeneratedRegex(@"/(?:dp|gp/product|gp/aw/d)/([A-Z0-9]{10})", RegexOptions.IgnoreCase)]
    private static partial Regex AmazonAsin();

    [GeneratedRegex(@"^(www|m)\.")]
    private static partial Regex HostPrefix();

    public static NormalizedUrl? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || !Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !uri.Host.Contains('.'))
        {
            return null;
        }

        var host = HostPrefix().Replace(uri.Host.ToLowerInvariant(), "");
        var key = Platforms.Keys.FirstOrDefault(k => host.Split('.').Contains(k));
        var platform = key is null ? host : Platforms[key];
        var path = uri.AbsolutePath.TrimEnd('/');

        if (key == "amazon")
        {
            var asin = AmazonAsin().Match(uri.AbsolutePath);
            if (asin.Success) return new($"https://www.{host}/dp/{asin.Groups[1].Value.ToUpperInvariant()}", platform);
        }
        if (key == "flipkart")
        {
            var pid = QueryHelpers.ParseQuery(uri.Query).TryGetValue("pid", out var v) ? v.ToString() : null;
            return new($"https://www.{host}{path}{(string.IsNullOrEmpty(pid) ? "" : $"?pid={Uri.EscapeDataString(pid)}")}", platform);
        }
        return new($"https://{host}{path}", platform);
    }
}
