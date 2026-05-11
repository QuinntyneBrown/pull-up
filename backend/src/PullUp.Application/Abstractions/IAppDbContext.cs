using Microsoft.EntityFrameworkCore;
using PullUp.Domain.Audit;
using PullUp.Domain.Users;

namespace PullUp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<AuditLogEntry> AuditLog { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
