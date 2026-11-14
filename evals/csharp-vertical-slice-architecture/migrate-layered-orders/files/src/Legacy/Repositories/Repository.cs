using Legacy.Data;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Repositories;

public interface IRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(int id);

    IQueryable<T> Query();

    Task AddAsync(T entity);

    Task SaveAsync();
}

public class Repository<T>(LegacyDbContext db) : IRepository<T>
    where T : class
{
    public async Task<T?> GetByIdAsync(int id) => await db.Set<T>().FindAsync(id);

    public IQueryable<T> Query() => db.Set<T>();

    public async Task AddAsync(T entity) => await db.Set<T>().AddAsync(entity);

    public Task SaveAsync() => db.SaveChangesAsync();
}
