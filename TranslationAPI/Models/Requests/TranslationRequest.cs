namespace TranslationAPI.Models.Requests;

public class TranslationRequest
{
    public string Text { get; set; } = string.Empty;
    public string FromLanguage { get; set; } = "auto";
    public string ToLanguage { get; set; } = "tr";
}
