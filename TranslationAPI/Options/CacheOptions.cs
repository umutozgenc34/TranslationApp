namespace TranslationAPI.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";

    public int AbsoluteExpirationMinutes { get; set; } = 5;
    public int SlidingExpirationMinutes { get; set; } = 2;
    public int MaxCacheEntries { get; set; } = 1000;
    public string KeyPrefix { get; set; } = "translation_";
}
