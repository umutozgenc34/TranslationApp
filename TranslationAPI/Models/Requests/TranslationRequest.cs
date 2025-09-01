using System.ComponentModel.DataAnnotations;

namespace TranslationAPI.Models.Requests;

public class TranslationRequest
{
    [Required(ErrorMessage = "Çevrilecek metin gerekli")]
    [StringLength(5000, ErrorMessage = "Metin 5000 karakterden uzun olamaz")]
    public string Text { get; set; } = string.Empty;

    public string FromLanguage { get; set; } = "auto";

    [Required(ErrorMessage = "Hedef dil gerekli")]
    public string ToLanguage { get; set; } = "tr";
}
