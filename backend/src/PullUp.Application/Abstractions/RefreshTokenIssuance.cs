using PullUp.Domain.Users;

namespace PullUp.Application.Abstractions;

// The raw value goes to the client; the record (which stores only the hash) is
// persisted by the calling handler.
public sealed record RefreshTokenIssuance(string RawToken, RefreshToken Record);
