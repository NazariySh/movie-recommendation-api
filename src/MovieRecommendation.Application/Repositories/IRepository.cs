using System.Linq.Expressions;
using MovieRecommendation.Domain.Entities;

namespace MovieRecommendation.Application.Repositories;

public interface IRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : BaseEntity
{
}

public interface IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
{
    TEntity Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task<TProjection?> GetAsync<TProjection>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TProjection?> GetSingleAsync<TProjection>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TProjection?> GetByIdAsync<TProjection>(TKey id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TProjection>> GetAllAsync<TProjection>(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TProjection>> GetAllAsync<TProjection>(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}
