using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using MovieRecommendation.Domain.Settings;

namespace MovieRecommendation.ML.Storage;

public class MlModelStorage
{
    private readonly MLContext _mlContext;
    private readonly ILogger<MlModelStorage> _logger;
    private readonly string _modelsPath;
    private readonly object _gate = new();

    private (string Version, ITransformer Model)? _cache;

    public MlModelStorage(
        MLContext mlContext,
        IOptions<RecommendationSettings> settings,
        ILogger<MlModelStorage> logger)
    {
        _mlContext = mlContext;
        _logger = logger;

        var configured = settings.Value.ModelStoragePath;
        _modelsPath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configured);
        Directory.CreateDirectory(_modelsPath);
    }

    public void SaveModel(ITransformer model, DataViewSchema schema, string version)
    {
        string path = GetModelPath(version);
        _mlContext.Model.Save(model, schema, path);
        _logger.LogInformation("ML model saved: {Version} → {Path}", version, path);

        lock (_gate)
        {
            _cache = (version, model);
        }
    }

    public ITransformer? LoadModel(string version)
    {
        lock (_gate)
        {
            if (_cache is { } current && current.Version == version)
            {
                return current.Model;
            }
        }

        string path = GetModelPath(version);
        if (!File.Exists(path))
        {
            _logger.LogWarning("ML model not found: {Path}", path);
            return null;
        }

        var loaded = _mlContext.Model.Load(path, out _);

        lock (_gate)
        {
            _cache = (version, loaded);
        }

        _logger.LogInformation("ML model loaded: {Version}", version);
        return loaded;
    }

    public string? GetLatestVersion()
    {
        string[] files = Directory.GetFiles(_modelsPath, "model_v*.zip");
        if (files.Length == 0)
        {
            return null;
        }

        return files
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name!.Replace("model_", ""))
            .OrderByDescending(v => v)
            .First();
    }

    public bool ModelExists(string version) =>
        File.Exists(GetModelPath(version));

    private string GetModelPath(string version) =>
        Path.Combine(_modelsPath, $"model_{version}.zip");
}
