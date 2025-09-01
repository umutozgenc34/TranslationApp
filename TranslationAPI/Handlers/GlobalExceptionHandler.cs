using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using TranslationAPI.Exceptions;
using TranslationAPI.Models.Responses;

namespace TranslationAPI.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var response = exception switch
        {
            BusinessException ex => CreateResponse(HttpStatusCode.BadRequest, ex.Message),
            ConfigurationException ex => CreateResponse(HttpStatusCode.InternalServerError, "Konfigürasyon hatası."),
            ExternalApiException ex => CreateResponse(HttpStatusCode.ServiceUnavailable, "Harici servis hatası."),
            HttpRequestException ex => CreateResponse(HttpStatusCode.ServiceUnavailable, "Ağ bağlantı hatası."),
            TaskCanceledException ex => CreateResponse(HttpStatusCode.RequestTimeout, "İstek zaman aşımına uğradı."),
            _ => CreateResponse(HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        _logger.LogError(exception, "Exception handled: {ExceptionType} - {Message}",
            exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = (int)response.StatusCode;
        httpContext.Response.ContentType = "application/json";

        var errorResponse = new ApiErrorResponse
        {
            Error = response.Message,
            StatusCode = (int)response.StatusCode,
            Timestamp = DateTime.UtcNow
        };

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }

    private static (HttpStatusCode StatusCode, string Message) CreateResponse(HttpStatusCode statusCode, string message)
        => (statusCode, message);
}
