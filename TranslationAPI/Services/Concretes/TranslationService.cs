using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using TranslationAPI.Models.External;
using TranslationAPI.Models.Requests;
using TranslationAPI.Models.Responses;
using TranslationAPI.Services.Abstracts;

namespace TranslationAPI.Services.Concretes;

public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationService> _logger;
    private readonly IConfiguration _configuration;

    private const string GOOGLE_TRANSLATE_URL = "https://translation.googleapis.com/language/translate/v2";

    private readonly Dictionary<string, string> _supportedLanguages = new()
        {
            { "auto", "Otomatik Algıla" },
            { "tr", "Türkçe" },
            { "en", "İngilizce" },
            { "de", "Almanca" },
            { "fr", "Fransızca" },
            { "es", "İspanyolca" },
            { "it", "İtalyanca" }
        };

    public TranslationService(HttpClient httpClient, IMemoryCache cache,
        ILogger<TranslationService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<TranslationResponse> TranslateAsync(TranslationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return new TranslationResponse
                {
                    Success = false,
                    Error = "Çevrilecek metin boş olamaz.",
                    Timestamp = DateTime.UtcNow
                };
            }

            var cacheKey = $"translation_{request.Text}_{request.FromLanguage}_{request.ToLanguage}";

            if (_cache.TryGetValue(cacheKey, out TranslationResponse? cachedResult))
            {
                cachedResult!.FromCache = true;
                return cachedResult;
            }

            var apiKey = _configuration["GoogleTranslate:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return new TranslationResponse
                {
                    Success = false,
                    Error = "API anahtarı yapılandırılmamış.",
                    Timestamp = DateTime.UtcNow
                };
            }

            var result = await CallGoogleTranslateAsync(request, apiKey);

            if (result.Success)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Çeviri işlemi hatası");
            return new TranslationResponse
            {
                Success = false,
                Error = "Çeviri işlemi başarısız.",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private async Task<TranslationResponse> CallGoogleTranslateAsync(TranslationRequest request, string apiKey)
    {
        try
        {
            var requestBody = new
            {
                q = request.Text,
                source = request.FromLanguage == "auto" ? null : request.FromLanguage,
                target = request.ToLanguage,
                format = "text"
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var url = $"{GOOGLE_TRANSLATE_URL}?key={apiKey}";

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var googleResponse = JsonSerializer.Deserialize<GoogleTranslateResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (googleResponse?.data?.translations?.Any() == true)
                {
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
            }

            return new TranslationResponse
            {
                Success = false,
                Error = $"API hatası: {response.StatusCode}",
                Timestamp = DateTime.UtcNow
            };
        }
        catch (HttpRequestException)
        {
            return new TranslationResponse
            {
                Success = false,
                Error = "Çeviri servisine bağlanılamadı.",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public async Task<List<string>> GetSupportedLanguagesAsync()
    {
        await Task.CompletedTask;
        return _supportedLanguages.Select(x => $"{x.Key}: {x.Value}").ToList();
    }
}