using FluentValidation;
using TranslationAPI.BusinessRules.Abstracts;
using TranslationAPI.BusinessRules.Concretes;
using TranslationAPI.Handlers;
using TranslationAPI.Services.Abstracts;
using TranslationAPI.Services.Concretes;

namespace TranslationAPI.Extensions;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependenciesExtension(this IServiceCollection services)
    {
        services.AddScoped<ITranslationService, TranslationService>();
        services.AddScoped<ITranslationBusinessRules, TranslationBusinessRules>();
        services.AddValidatorsFromAssemblyContaining<TranslationAppAssembly>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}
