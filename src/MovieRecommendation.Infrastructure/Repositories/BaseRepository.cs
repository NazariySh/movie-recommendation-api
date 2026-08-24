using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using MovieRecommendation.Application.Repositories;
using MovieRecommendation.Domain.Entities;
using MovieRecommendation.Infrastructure.Data;

namespace MovieRecommendation.Infrastructure.Repositories;

public class BaseRepository<TEntity> : BaseRepository<TEntity, Guid>, IRepository<TEntity>
    where TEntity : BaseEntity, new()
{
    public BaseRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
        : base(dbContext, mapperConfiguration)
    {
    }
}

public class BaseRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    private readonly DbSet<TEntity> _dbSet;

    protected readonly ApplicationDbContext DbContext;
    protected readonly IConfigurationProvider MapperConfiguration;

    public BaseRepository(ApplicationDbContext dbContext, IConfigurationProvider mapperConfiguration)
    {
        DbContext = dbContext;
        MapperConfiguration = mapperConfiguration;
        _dbSet = dbContext.Set<TEntity>();
    }

    public TEntity Add(TEntity entity)
    {
        return DbContext.Set<TEntity>().Add(entity).Entity;
    }

    public void Update(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }

    public void DetachRange(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            DbContext.Entry(entity).State = EntityState.Detached;
        }
    }

    public Task<TProjection?> GetAsync<TProjection>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return GetNoTrackingQueryable(predicate)
            .ProjectTo<TProjection>(MapperConfiguration)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TProjection?> GetSingleAsync<TProjection>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return GetNoTrackingQueryable(predicate)
            .ProjectTo<TProjection>(MapperConfiguration)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<TProjection?> GetByIdAsync<TProjection>(TKey id, CancellationToken cancellationToken = default)
    {
        return GetSingleAsync<TProjection>(
            x => x.Id.Equals(id),
            cancellationToken);
    }

    public async Task<IReadOnlyList<TProjection>> GetAllAsync<TProjection>(CancellationToken cancellationToken = default)
    {
        return await GetNoTrackingQueryable()
            .ProjectTo<TProjection>(MapperConfiguration)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TProjection>> GetAllAsync<TProjection>(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return await GetNoTrackingQueryable(predicate)
            .ProjectTo<TProjection>(MapperConfiguration)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    private IQueryable<TEntity> GetNoTrackingQueryable(Expression<Func<TEntity, bool>>? predicate = null)
    {
        return GetQueryable(predicate).AsNoTracking();
    }

    private IQueryable<TEntity> GetQueryable(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = _dbSet.AsQueryable();

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return query;
    }
}
