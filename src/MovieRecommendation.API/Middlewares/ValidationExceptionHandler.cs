using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace MovieRecommendation.API.Middlewares;

internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ValidationExceptionHandler> _logger;

    public ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        _logger.LogWarning("Validation exception: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        var problemDetails = new ValidationProblemDetails(GetValidationErrors(validationException))
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status422UnprocessableEntity),
            Type = nameof(ValidationException),
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier }
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static Dictionary<string, string[]> GetValidationErrors(ValidationException exception)
    {
        return exception.Errors
            .GroupBy(e => NormalizePropertyName(e.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());
    }

    private static string NormalizePropertyName(string propertyName)
    {
        const string ModelPrefix = "Model.";
        return propertyName.StartsWith(ModelPrefix, StringComparison.Ordinal)
            ? propertyName[ModelPrefix.Length..]
            : propertyName;
    }
}
