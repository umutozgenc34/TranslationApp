using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TranslationAPI.BusinessRules.Abstracts;
using TranslationAPI.Models.External;
using TranslationAPI.Models.Requests;
using TranslationAPI.Models.Responses;
using TranslationAPI.Options;
using TranslationAPI.Services.Abstracts;

namespace TranslationAPI.Services.Concretes;

public class TranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationService> _logger;
    private readonly ITranslationBusinessRules _businessRules;
    private readonly GoogleTranslateOptions _googleOptions;
    private readonly CacheOptions _cacheOptions;

    public TranslationService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<TranslationService> logger,
        ITranslationBusinessRules businessRules,
        IOptions<GoogleTranslateOptions> googleOptions,
        IOptions<CacheOptions> cacheOptions)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _businessRules = businessRules;
        _googleOptions = googleOptions.Value;
        _cacheOptions = cacheOptions.Value;

        _httpClient.Timeout = TimeSpan.FromSeconds(_googleOptions.TimeoutSeconds);
    }

    public async Task<TranslationResponse> TranslateAsync(TranslationRequest request)
    {
        await _businessRules.ValidateTranslationRequestAsync(request);

        var cachedResult = await GetFromCacheAsync(request);
        if (cachedResult != null)
        {
            _logger.LogInformation("Translation returned from cache for: {Text}",
                request.Text[..Math.Min(50, request.Text.Length)]);
            return cachedResult;
        }

        var result = await CallGoogleTranslateAsync(request);

        if (result.Success)
        {
            await CacheResultAsync(request, result);
        }

        return result;
    }

    private async Task<TranslationResponse?> GetFromCacheAsync(TranslationRequest request)
    {
        var cacheKey = _businessRules.GenerateCacheKey(request, _cacheOptions.KeyPrefix);

        if (_cache.TryGetValue(cacheKey, out TranslationResponse? cachedResult))
        {
            cachedResult!.FromCache = true;
            return cachedResult;
        }

        return await Task.FromResult<TranslationResponse?>(null);
    }

    private async Task CacheResultAsync(TranslationRequest request, TranslationResponse result)
    {
        var cacheKey = _businessRules.GenerateCacheKey(request, _cacheOptions.KeyPrefix);
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.AbsoluteExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.SlidingExpirationMinutes),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, result, cacheEntryOptions);
        _logger.LogInformation("Translation cached with key: {CacheKey}", cacheKey);

        await Task.CompletedTask;
    }

    private async Task<TranslationResponse> CallGoogleTranslateAsync(TranslationRequest request)
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
        var url = $"{_googleOptions.BaseUrl}?key={_googleOptions.ApiKey}";

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