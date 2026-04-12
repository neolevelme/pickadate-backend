using Pickadate.BuildingBlocks.Application;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly PickadateDbContext _db;
    public UnitOfWork(PickadateDbContext db) => _db = db;
    public Task<int> CommitAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
