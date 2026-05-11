using Microsoft.EntityFrameworkCore;
using PullUp.Domain.Users;

namespace PullUp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
