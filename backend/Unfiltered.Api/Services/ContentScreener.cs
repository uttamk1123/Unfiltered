using System.Text.RegularExpressions;

namespace Unfiltered.Api.Services;

/// <summary>
/// Rule-based pre-screen that runs before a review reaches the human moderation queue.
/// It redacts personal data and leaves notes for the moderator; it never rejects on its own.
/// Replace with an AI classifier later (PRD feature F7).
/// </summary>
public static partial class ContentScreener
{
    [GeneratedRegex(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)+")]
    private static partial Regex Email();

    [GeneratedRegex(@"(?<![\w-])(?:\+?\d{1,3}[\s-]?)?(?:\d[\s-]?){9}\d(?![\w-])")]
    private static partial Regex LongNumber();

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex Link();

    [GeneratedRegex(@"\b(fraud|scam|scammers?|cheat(?:ed|ers?|ing)?|thie(?:f|ves)|criminals?|illegal|stole|fake company)\b", RegexOptions.IgnoreCase)]
    private static partial Regex StrongAllegation();

    [GeneratedRegex(@"\b(idiot|stupid|moron|bastard|scumbag)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Abuse();

    public record Result(string Title, string Body, IReadOnlyList<string> Notes);

    public static Result Screen(string title, string body)
    {
        var (cleanTitle, titleCount) = Redact(title);
        var (cleanBody, bodyCount) = Redact(body);
        var notes = new List<string>();

        if (titleCount + bodyCount > 0)
            notes.Add($"Removed {titleCount + bodyCount} email address(es) or phone/ID number(s).");

        var all = $"{cleanTitle} {cleanBody}";
        if (StrongAllegation().IsMatch(all))
            notes.Add("Strong allegation (fraud, scam, theft...): check the proof supports it, or ask the reviewer to word it as their experience.");
        if (Abuse().IsMatch(all))
            notes.Add("Possible abusive language.");
        if (Link().IsMatch(all))
            notes.Add("Contains a link: check it is not spam or promotion.");

        var letters = all.Where(char.IsAsciiLetter).ToArray();
        if (letters.Length > 40 && letters.Count(char.IsAsciiLetterUpper) / (double)letters.Length > 0.6)
            notes.Add("Mostly capital letters.");

        return new Result(cleanTitle, cleanBody, notes);
    }

    private static (string Text, int Count) Redact(string text)
    {
        var count = 0;
        text = Email().Replace(text, _ => { count++; return "[email removed]"; });
        text = LongNumber().Replace(text, _ => { count++; return "[number removed]"; });
        return (text, count);
    }
}
