using PGLN.Auth.Application.Abstractions.Time;

namespace PGLN.Auth.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow =>
        DateTimeOffset.UtcNow;
}
