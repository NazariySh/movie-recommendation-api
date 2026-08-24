using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.ML.BackgroundServices;

public class ModelRetrainingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RecommendationSettings _settings;
    private readonly ILogger<ModelRetrainingHostedService> _logger;

    public ModelRetrainingHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RecommendationSettings> settings,
        ILogger<ModelRetrainingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("ModelRetrainingService started");

        await EnsureInitialModelAsync(ct);

        var interval = TimeSpan.FromDays(Math.Max(1, _settings.RetrainingIntervalDays));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);
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
                await orchestrator.RunAsync(cancellationToken: ct);
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
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, _settings.InitialTrainingDelaySeconds)), ct);
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
            await orchestrator.RunAsync(cancellationToken: ct);
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
