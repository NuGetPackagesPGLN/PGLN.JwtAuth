using PGLN.Auth.Infrastructure.Time;

namespace PGLN.Auth.Infrastructure.Tests.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        var clock =
            new SystemClock();

        var before =
            DateTimeOffset.UtcNow;

        var actual =
            clock.UtcNow;

        var after =
            DateTimeOffset.UtcNow;

        Assert.InRange(
            actual,
            before,
            after);

        Assert.Equal(
            TimeSpan.Zero,
            actual.Offset);
    }
}
