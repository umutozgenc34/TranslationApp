namespace TranslationAPI.Options;

public class GoogleTranslateOptions
{
    public const string SectionName = "GoogleTranslate";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://translation.googleapis.com/language/translate/v2";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int MaxTextLength { get; set; } = 5000;
    public int MaxBatchSize { get; set; } = 10;
}
