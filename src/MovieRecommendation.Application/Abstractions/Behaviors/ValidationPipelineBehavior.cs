using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace MovieRecommendation.Application.Abstractions.Behaviors;

internal sealed class ValidationPipelineBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ValidationFailure[] validationFailures = await ValidateAsync(request);

        if (validationFailures.Length > 0)
        {
            throw new ValidationException(validationFailures);
        }

        return await next(cancellationToken);
    }

    private async Task<ValidationFailure[]> ValidateAsync(TRequest request)
    {
        if (!validators.Any())
        {
            return [];
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context)));

        return validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToArray();
    }
}
