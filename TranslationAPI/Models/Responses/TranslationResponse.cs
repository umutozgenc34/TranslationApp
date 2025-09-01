namespace TranslationAPI.Models.Responses;

public class TranslationResponse
{
    public bool Success { get; set; }
    public string? TranslatedText { get; set; }
    public string? OriginalText { get; set; }
    public string? FromLanguage { get; set; }
    public string? ToLanguage { get; set; }
    public string? Error { get; set; }
    public bool FromCache { get; set; }
    public DateTime Timestamp { get; set; }
}