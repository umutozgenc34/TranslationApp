using FluentValidation;
using TranslationAPI.BusinessRules.Abstracts;
using TranslationAPI.BusinessRules.Concretes;
using TranslationAPI.Handlers;
using TranslationAPI.Services.Abstracts;
using TranslationAPI.Services.Concretes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<ITranslationService, TranslationService>();

builder.Services.AddScoped<ITranslationBusinessRules, TranslationBusinessRules>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();
app.UseCors("AllowFrontend");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();