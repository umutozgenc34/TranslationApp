using Microsoft.AspNetCore.Mvc;
using TranslationAPI.Models.Requests;
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
    public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
    {
        var result = await _translationService.TranslateAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("translate/batch")]
    public async Task<IActionResult> TranslateBatch([FromBody] List<TranslationRequest> requests)
    {
        if (requests?.Count > 10)
            return BadRequest("Maksimum 10 metin çevrilebilir.");

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
