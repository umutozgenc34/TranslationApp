using Microsoft.Extensions.Caching.Memory;
using System.Text;
using TranslationAPI.BusinessRules.Abstracts;
using TranslationAPI.Exceptions;
using TranslationAPI.Models.External;
using TranslationAPI.Models.Requests;
using TranslationAPI.Models.Responses;

namespace TranslationAPI.BusinessRules.Concretes;

public class TranslationBusinessRules : ITranslationBusinessRules
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, string> _supportedLanguages;

    public TranslationBusinessRules(IConfiguration configuration)
    {
        _configuration = configuration;
        _supportedLanguages = new()
        {
            { "auto", "Otomatik Algıla" },
            { "tr", "Türkçe" },
            { "en", "İngilizce" },
            { "de", "Almanca" },
            { "fr", "Fransızca" },
            { "es", "İspanyolca" },
            { "it", "İtalyanca" }
        };
    }

    public async Task ValidateTranslationRequestAsync(TranslationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            throw new BusinessException("Çevrilecek metin boş olamaz.");

        if (request.Text.Length > 5000)
            throw new BusinessException("Metin 5000 karakterden uzun olamaz.");

        if (!_supportedLanguages.ContainsKey(request.FromLanguage.ToLower()))
            throw new BusinessException($"Desteklenmeyen kaynak dil: {request.FromLanguage}");

        if (!_supportedLanguages.ContainsKey(request.ToLanguage.ToLower()))
            throw new BusinessException($"Desteklenmeyen hedef dil: {request.ToLanguage}");

        await Task.CompletedTask;
    }

    public async Task<string> GetValidApiKeyAsync()
    {
        var apiKey = _configuration["GoogleTranslate:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
            throw new ConfigurationException("Google Translate API anahtarı yapılandırılmamış.");

        return await Task.FromResult(apiKey);
    }

    public string GenerateCacheKey(TranslationRequest request)
    {
        var input = $"{request.Text}_{request.FromLanguage}_{request.ToLanguage}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return $"translation_{Convert.ToBase64String(hash)[..16]}";
    }

    public MemoryCacheEntryOptions GetCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };
    }

    public TranslationResponse CreateErrorResponse(string error)
    {
        return new TranslationResponse
        {
            Success = false,
            Error = error,
            FromCache = false,
            Timestamp = DateTime.UtcNow
        };
    }

    public TranslationResponse ProcessGoogleTranslateResponse(GoogleTranslateResponse? googleResponse, TranslationRequest request)
    {
        if (googleResponse?.data?.translations?.Any() != true)
        {
            throw new ExternalApiException("Google Translate API'sinden geçersiz yanıt alındı.");
        }

        var translation = googleResponse.data.translations.First();

        return new TranslationResponse
        {
            Success = true,
            TranslatedText = translation.translatedText,
            OriginalText = request.Text,
            FromLanguage = translation.detectedSourceLanguage ?? request.FromLanguage,
            ToLanguage = request.ToLanguage,
            FromCache = false,
            Timestamp = DateTime.UtcNow
        };
    }

    public List<string> GetSupportedLanguages()
    {
        return _supportedLanguages.Select(x => $"{x.Key}: {x.Value}").ToList();
    }
}
