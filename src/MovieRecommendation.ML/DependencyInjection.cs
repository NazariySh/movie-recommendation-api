using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using MovieRecommendation.Application.Interfaces.ML;
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
    public static IServiceCollection AddMlServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddSingleton<MLContext>();

        var openAiKey = config["OpenAI:ApiKey"]
            ?? throw new ArgumentException("OpenAI:ApiKey is missing");

        services.AddSingleton(_ => new EmbeddingClient(EmbeddingGenerator.Model, openAiKey));

        services.AddSingleton<MlModelStorage>();
        services.AddScoped<RecommendationModelTrainer>();
        services.AddSingleton<IModelRetrainingOrchestrator, ModelRetrainingOrchestrator>();

        services.AddSingleton<CollaborativeFilteringPredictor>();

        services.AddSingleton<VectorSimilarityService>();

        services.AddScoped<IEmbeddingService, EmbeddingGenerator>();
        services.AddScoped<ISearchEngine, SemanticSearchEngine>();
        services.AddScoped<IRecommendationEngine, HybridRecommender>();
        services.AddScoped<ColdStartRecommender>();

        services.AddHostedService<ModelRetrainingHostedService>();

        return services;
    }
}
