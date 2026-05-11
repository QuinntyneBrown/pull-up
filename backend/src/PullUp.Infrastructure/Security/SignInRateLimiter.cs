using Microsoft.Extensions.Caching.Memory;
using PullUp.Application.Abstractions;
using PullUp.Application.Common.Exceptions;

namespace PullUp.Infrastructure.Security;

// In-memory sliding-window counter keyed by the normalized email. Five attempts
// in 60 seconds locks the email out for the remainder of the window; the next
// successful sign-in clears the counter. Per-process state; for multi-instance
// deployments this would be backed by Redis.
public sealed class SignInRateLimiter : ISignInRateLimiter
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    private readonly IMemoryCache _cache;

    public SignInRateLimiter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void EnsureNotLocked(string email)
    {
        var key = Key(email);
        if (_cache.TryGetValue<int>(key, out var attempts) && attempts >= MaxAttempts)
        {
            throw new SignInRateLimitExceededException((int)Window.TotalSeconds);
        }
    }

    public void RegisterFailedAttempt(string email)
    {
        var key = Key(email);
        var attempts = _cache.TryGetValue<int>(key, out var existing) ? existing + 1 : 1;
        _cache.Set(key, attempts, Window);
    }

    public void ResetAttempts(string email)
    {
        _cache.Remove(Key(email));
    }

    private static string Key(string email) => $"signin:{email.Trim().ToLowerInvariant()}";
}
