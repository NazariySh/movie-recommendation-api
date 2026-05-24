using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace MovieRecommendation.ML.Storage;

public class MlModelStorage
{
    private readonly MLContext _mlContext;
    private readonly ILogger<MlModelStorage> _logger;
    private readonly string _modelsPath;

    private ITransformer? _cachedModel;
    private string? _cachedVersion;

    public MlModelStorage(MLContext mlContext, ILogger<MlModelStorage> logger)
    {
        _mlContext = mlContext;
        _logger = logger;
        _modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModels");
        Directory.CreateDirectory(_modelsPath);
    }

    public void SaveModel(ITransformer model, DataViewSchema schema, string version)
    {
        string path = GetModelPath(version);
        _mlContext.Model.Save(model, schema, path);
        _logger.LogInformation("ML model saved: {Version} → {Path}", version, path);

        _cachedModel = model;
        _cachedVersion = version;
    }

    public ITransformer? LoadModel(string version)
    {
        if (_cachedVersion == version && _cachedModel != null)
        {
            return _cachedModel;
        }

        string path = GetModelPath(version);
        if (!File.Exists(path))
        {
            _logger.LogWarning("ML model not found: {Path}", path);
            return null;
        }

        _cachedModel = _mlContext.Model.Load(path, out _);
        _cachedVersion = version;

        _logger.LogInformation("ML model loaded: {Version}", version);
        return _cachedModel;
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
