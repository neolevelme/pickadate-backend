using Microsoft.EntityFrameworkCore;
using Pickadate.Domain.Users;
using Pickadate.Infrastructure.Persistence;

namespace Pickadate.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly PickadateDbContext _db;
    public UserRepository(PickadateDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
    }
}
