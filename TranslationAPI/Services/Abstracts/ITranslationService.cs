using TranslationAPI.Models.Requests;
using TranslationAPI.Models.Responses;

namespace TranslationAPI.Services.Abstracts;

public interface ITranslationService
{
    Task<TranslationResponse> TranslateAsync(TranslationRequest request);
    Task<List<string>> GetSupportedLanguagesAsync();
}
