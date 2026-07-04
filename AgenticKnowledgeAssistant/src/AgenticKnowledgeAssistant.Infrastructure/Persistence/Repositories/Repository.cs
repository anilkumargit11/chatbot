using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgenticKnowledgeAssistant.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgenticKnowledgeAssistant.Infrastructure.Persistence.Repositories;

public class Repository<T>(ApplicationDbContext dbContext) : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext DbContext = dbContext;

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<T>().AddAsync(entity, cancellationToken);
    }

    public void Update(T entity)
    {
        DbContext.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        DbContext.Set<T>().Remove(entity);
    }
}
