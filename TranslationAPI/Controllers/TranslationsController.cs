using Microsoft.AspNetCore.Mvc;
using TranslationAPI.Exceptions;
using TranslationAPI.Models.Requests;
using TranslationAPI.Models.Responses;
using TranslationAPI.Services.Abstracts;

namespace TranslationAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<TranslationController> _logger;

    public TranslationController(ITranslationService translationService, ILogger<TranslationController> logger)
    {
        _translationService = translationService;
        _logger = logger;
    }

    [HttpPost("translate")]
    [ProducesResponseType(typeof(TranslationResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
    {
        _logger.LogInformation("Translation request received for text: {TextPreview}",
            request.Text?[..Math.Min(50, request.Text?.Length ?? 0)]);

        var result = await _translationService.TranslateAsync(request);
        return Ok(result);
    }

    [HttpPost("translate/batch")]
    [ProducesResponseType(typeof(List<TranslationResponse>), 200)]
    public async Task<IActionResult> TranslateBatch([FromBody] List<TranslationRequest> requests)
    {
        if (requests?.Count > 10)
            throw new BusinessException("Maksimum 10 metin aynı anda çevrilebilir.");

        var tasks = requests!.Select(req => _translationService.TranslateAsync(req));
        var results = await Task.WhenAll(tasks);

        return Ok(results);
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetSupportedLanguages()
    {
        var languages = await _translationService.GetSupportedLanguagesAsync();
        return Ok(languages);
    }
}