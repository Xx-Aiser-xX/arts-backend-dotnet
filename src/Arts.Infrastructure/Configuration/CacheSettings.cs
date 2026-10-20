namespace Arts.Infrastructure.Configuration;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public int DefaultTtlMinutes { get; set; } = 10;
    
    public Dictionary<string, int> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public TimeSpan GetTtl(string policyName)
    {
        if (Policies.TryGetValue(policyName, out var minutes))
            return TimeSpan.FromMinutes(minutes);

        return TimeSpan.FromMinutes(DefaultTtlMinutes);
    }
}

public static class CachePolicy
{
    public const string Arts = "Arts";
    public const string Comments = "Comments";
    public const string Search = "Search";
    public const string AuthorsArts = "AuthorsArts";
    public const string Tags = "Tags";
    public const string SocialNetworks = "SocialNetworks";
    public const string User = "User";
}