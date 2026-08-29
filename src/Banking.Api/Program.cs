using System.Text.Json.Serialization;
using Banking.Api.Middleware;
using Banking.Api.Serialization;
using Banking.Application;
using Banking.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        // Lesbare Enum-Werte ("Active" statt 0) - konsistent mit der Speicherung als string in der DB.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ProblemDetails (RFC 7807) für alle Fehlerantworten - auch für Fälle, die [ApiController]
// selbst erzeugt (z. B. ungültiges Model Binding), nicht nur für unsere eigenen.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Globaler Exception Handler muss so früh wie möglich in der Pipeline stehen, damit er
// Exceptions aus allen nachfolgenden Middlewares/Controllern abfangen kann.
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
