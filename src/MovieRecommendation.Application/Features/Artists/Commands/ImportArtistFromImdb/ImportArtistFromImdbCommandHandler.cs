using Microsoft.Extensions.Logging;
using MovieRecommendation.Application.Abstractions.Messaging;
using MovieRecommendation.Application.Features.Artists.Common;
using MovieRecommendation.Application.Interfaces;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities.Movies;
using MovieRecommendation.Domain.Exceptions;

namespace MovieRecommendation.Application.Features.Artists.Commands.ImportArtistFromImdb;

public class ImportArtistFromImdbCommandHandler : ICommandHandler<ImportArtistFromImdbCommand, Guid>
{
    private readonly IArtistRepository _artistRepository;
    private readonly IArtistSlugGenerator _slugGenerator;
    private readonly ITmdbPersonImporter _tmdbImporter;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImportArtistFromImdbCommandHandler> _logger;

    public ImportArtistFromImdbCommandHandler(
        IArtistRepository artistRepository,
        IArtistSlugGenerator slugGenerator,
        ITmdbPersonImporter tmdbImporter,
        IBlobStorageService blobStorage,
        IUnitOfWork unitOfWork,
        ILogger<ImportArtistFromImdbCommandHandler> logger)
    {
        _artistRepository = artistRepository;
        _slugGenerator = slugGenerator;
        _tmdbImporter = tmdbImporter;
        _blobStorage = blobStorage;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(ImportArtistFromImdbCommand request, CancellationToken cancellationToken)
    {
        var imdbId = request.Model.ImdbId.Trim();

        var existing = await _artistRepository.GetByImdbIdAsync(imdbId, cancellationToken);
        if (existing is not null)
        {
            throw new AlreadyExistsException($"Artist with IMDb id {imdbId} already exists");
        }

        var fetched = await _tmdbImporter.FetchByImdbIdAsync(imdbId, cancellationToken)
            ?? throw new NotFoundException($"No TMDb person found for IMDb id {imdbId}");

        var slug = await _slugGenerator.GenerateUniqueAsync(fetched.Name, excludeId: null, cancellationToken);

        var photoUrl = fetched.ProfileImageUrl;
        if (!string.IsNullOrWhiteSpace(photoUrl))
        {
            try
            {
                var path = $"artists/{slug}{Path.GetExtension(new Uri(photoUrl).AbsolutePath)}";
                photoUrl = await _blobStorage.CopyFromUrlAsync(photoUrl, path, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mirror TMDb photo for {ImdbId}; storing source URL", imdbId);
            }
        }

        var artist = new Person
        {
            Slug = slug,
            Name = fetched.Name,
            ImdbId = fetched.ImdbId,
            TmdbId = fetched.TmdbId,
            Biography = fetched.Biography,
            Birthday = fetched.Birthday,
            DateOfDeath = fetched.DateOfDeath,
            PlaceOfBirth = fetched.PlaceOfBirth,
            KnownForDepartment = fetched.KnownForDepartment,
            Gender = fetched.Gender,
            PhotoUrl = photoUrl,
        };

        _artistRepository.Add(artist);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Imported artist {ArtistId} from IMDb {ImdbId}", artist.Id, imdbId);

        return artist.Id;
    }
}
