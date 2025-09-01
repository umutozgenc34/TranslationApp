namespace TranslationAPI.Models.External;

public class Translation
{
    public string translatedText { get; set; } = string.Empty;
    public string detectedSourceLanguage { get; set; } = string.Empty;
}