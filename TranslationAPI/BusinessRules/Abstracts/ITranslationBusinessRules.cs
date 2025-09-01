using Microsoft.Extensions.Caching.Memory;
using TranslationAPI.Models.External;
using TranslationAPI.Models.Requests;
using TranslationAPI.Models.Responses;

namespace TranslationAPI.BusinessRules.Abstracts;

public interface ITranslationBusinessRules
{
    Task ValidateTranslationRequestAsync(TranslationRequest request);
    Task<string> GetValidApiKeyAsync();
    string GenerateCacheKey(TranslationRequest request);
    MemoryCacheEntryOptions GetCacheOptions();
    TranslationResponse CreateErrorResponse(string error);
    TranslationResponse ProcessGoogleTranslateResponse(GoogleTranslateResponse? googleResponse, TranslationRequest request);
    List<string> GetSupportedLanguages();
}
