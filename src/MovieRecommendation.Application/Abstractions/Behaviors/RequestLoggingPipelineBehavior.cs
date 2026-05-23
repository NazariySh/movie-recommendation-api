using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace MovieRecommendation.Application.Abstractions.Behaviors;

internal sealed class RequestLoggingPipelineBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing request {RequestName}", requestName);

        try
        {
            TResponse result = await next(cancellationToken);

            logger.LogInformation("Completed request {RequestName}", requestName);

            return result;
        }
        catch (Exception exception)
        {
            using (LogContext.PushProperty("Error", exception, true))
            {
                logger.LogError(exception, "Completed request {RequestName} with error", requestName);
            }

            throw;
        }
    }
}
