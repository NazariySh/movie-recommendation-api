using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;

namespace MovieRecommendation.ML.BackgroundServices;

public class ModelRetrainingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModelRetrainingHostedService> _logger;

    private static readonly TimeSpan RetrainingInterval = TimeSpan.FromDays(7);
    private static readonly TimeSpan InitialTrainingDelay = TimeSpan.FromSeconds(15);

    public ModelRetrainingHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ModelRetrainingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ModelRetrainingService started");

        await EnsureInitialModelAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(RetrainingInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider
                    .GetRequiredService<IModelRetrainingOrchestrator>();
                await orchestrator.RunAsync(ct);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Recurring retrain skipped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recurring model retraining");
            }
        }
    }

    private async Task EnsureInitialModelAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(InitialTrainingDelay, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var metadataRepo = scope.ServiceProvider.GetRequiredService<IMlModelMetadataRepository>();

            var active = await metadataRepo.GetActiveAsync(ct);
            if (active is not null)
            {
                _logger.LogInformation(
                    "Active recommendation model present: {Version} (RMSE {Rmse:F4})",
                    active.Version,
                    active.Rmse);
                return;
            }

            _logger.LogInformation("No active recommendation model found; starting initial training");

            var orchestrator = scope.ServiceProvider.GetRequiredService<IModelRetrainingOrchestrator>();
            await orchestrator.RunAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Initial training skipped (insufficient ratings); will retry on next interval");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial model training failed");
        }
    }
}
