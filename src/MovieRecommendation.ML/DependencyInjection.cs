using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using MovieRecommendation.Application.Interfaces.ML;
using MovieRecommendation.Domain.Settings;
using MovieRecommendation.ML.BackgroundServices;
using MovieRecommendation.ML.Embeddings;
using MovieRecommendation.ML.Predictors;
using MovieRecommendation.ML.Recommenders;
using MovieRecommendation.ML.Storage;
using MovieRecommendation.ML.Trainers;
using OpenAI.Embeddings;

namespace MovieRecommendation.ML;

public static class DependencyInjection
{
    public static IServiceCollection AddMlServices(this IServiceCollection services)
    {
        services.AddSingleton<MLContext>();

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<OpenAiSettings>>().Value;
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException("OpenAI:ApiKey is missing");
            }
            return new EmbeddingClient(settings.EmbeddingModel, settings.ApiKey);
        });

        services.AddSingleton<MlModelStorage>();
        services.AddScoped<RecommendationModelTrainer>();
        services.AddSingleton<IModelRetrainingOrchestrator, ModelRetrainingOrchestrator>();

        services.AddSingleton<CollaborativeFilteringPredictor>();

        services.AddSingleton<VectorSimilarityService>();
        services.AddSingleton<MmrReRanker>();

        services.AddScoped<IEmbeddingService, EmbeddingGenerator>();
        services.AddScoped<ISearchEngine, SemanticSearchEngine>();
        services.AddScoped<IRecommendationEngine, HybridRecommender>();
        services.AddScoped<ColdStartRecommender>();

        services.AddHostedService<ModelRetrainingHostedService>();

        return services;
    }
}
