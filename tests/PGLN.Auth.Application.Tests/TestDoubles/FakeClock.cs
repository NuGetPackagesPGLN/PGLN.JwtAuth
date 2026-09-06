using PGLN.Auth.Application.Abstractions.Time;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}
