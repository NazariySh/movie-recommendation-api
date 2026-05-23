using System.Text.Json.Serialization;
using MovieRecommendation.API.Extensions;
using MovieRecommendation.API.Middlewares;
using MovieRecommendation.Application;
using MovieRecommendation.Infrastructure;
using MovieRecommendation.ML;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMlServices(builder.Configuration);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    {
        var clientUrl = builder.Configuration["Client:Url"];
        ArgumentException.ThrowIfNullOrEmpty(clientUrl);

        policy.WithOrigins(clientUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }));

builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.ConfigureAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.ConfigureRateLimiter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
