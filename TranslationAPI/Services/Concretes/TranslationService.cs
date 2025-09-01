using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using TranslationAPI.BusinessRules.Abstracts;
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
    private readonly ITranslationBusinessRules _businessRules;

    private const string GOOGLE_TRANSLATE_URL = "https://translation.googleapis.com/language/translate/v2";

    public TranslationService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<TranslationService> logger,
        IConfiguration configuration,
        ITranslationBusinessRules businessRules)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        _businessRules = businessRules;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<TranslationResponse> TranslateAsync(TranslationRequest request)
    {
        await _businessRules.ValidateTranslationRequestAsync(request);

        var cachedResult = await GetFromCacheAsync(request);
        if (cachedResult != null)
        {
            _logger.LogInformation("Translation returned from cache for: {Text}", request.Text[..Math.Min(50, request.Text.Length)]);
            return cachedResult;
        }

        var apiKey = await _businessRules.GetValidApiKeyAsync();

        var result = await CallGoogleTranslateAsync(request, apiKey);

        if (result.Success)
        {
            await CacheResultAsync(request, result);
        }

        return result;
    }

    private async Task<TranslationResponse?> GetFromCacheAsync(TranslationRequest request)
    {
        var cacheKey = _businessRules.GenerateCacheKey(request);

        if (_cache.TryGetValue(cacheKey, out TranslationResponse? cachedResult))
        {
            cachedResult!.FromCache = true;
            return cachedResult;
        }

        return await Task.FromResult<TranslationResponse?>(null);
    }

    private async Task CacheResultAsync(TranslationRequest request, TranslationResponse result)
    {
        var cacheKey = _businessRules.GenerateCacheKey(request);
        var cacheOptions = _businessRules.GetCacheOptions();

        _cache.Set(cacheKey, result, cacheOptions);
        _logger.LogInformation("Translation cached with key: {CacheKey}", cacheKey);

        await Task.CompletedTask;
    }

    private async Task<TranslationResponse> CallGoogleTranslateAsync(TranslationRequest request, string apiKey)
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

        _logger.LogInformation("Calling Google Translate API: {FromLang} -> {ToLang}",
            request.FromLanguage, request.ToLanguage);

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Google Translate API error: {StatusCode} - {Error}",
                response.StatusCode, errorContent);

            return _businessRules.CreateErrorResponse($"API hatası: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var googleResponse = JsonSerializer.Deserialize<GoogleTranslateResponse>(responseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return _businessRules.ProcessGoogleTranslateResponse(googleResponse, request);
    }

    public async Task<List<string>> GetSupportedLanguagesAsync()
    {
        var languages = _businessRules.GetSupportedLanguages();
        return await Task.FromResult(languages);
    }
}