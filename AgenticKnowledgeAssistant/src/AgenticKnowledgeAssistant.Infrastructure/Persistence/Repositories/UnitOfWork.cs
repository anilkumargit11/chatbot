using System.Threading;
using System.Threading.Tasks;
using AgenticKnowledgeAssistant.Domain.Repositories;

namespace AgenticKnowledgeAssistant.Infrastructure.Persistence.Repositories;

public class UnitOfWork(ApplicationDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }
}
