using FluentValidation;
using TranslationAPI.Models.Requests;

namespace TranslationAPI.Validators;

public class TranslationRequestValidator : AbstractValidator<TranslationRequest>
{
    private readonly List<string> _validLanguages = new()
    { "auto", "tr", "en", "de", "fr", "es", "it" };

    public TranslationRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Çevrilecek metin boş olamaz")
            .MaximumLength(5000).WithMessage("Metin 5000 karakterden uzun olamaz");

        RuleFor(x => x.FromLanguage)
            .Must(x => _validLanguages.Contains(x.ToLower()))
            .WithMessage("Geçersiz kaynak dil kodu");

        RuleFor(x => x.ToLanguage)
            .NotEmpty().WithMessage("Hedef dil gerekli")
            .Must(x => _validLanguages.Contains(x.ToLower()))
            .WithMessage("Geçersiz hedef dil kodu");
    }
}