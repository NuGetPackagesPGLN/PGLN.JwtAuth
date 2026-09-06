namespace PGLN.Auth.Application.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}