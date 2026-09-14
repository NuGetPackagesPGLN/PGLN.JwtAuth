namespace PGLN.Auth.Domain.ExternalLogins;

public readonly record struct ExternalLoginId(Guid Value)
{
    public static ExternalLoginId New() =>
        new(Guid.NewGuid());

    public static implicit operator Guid(
        ExternalLoginId id) =>
        id.Value;

    public static explicit operator ExternalLoginId(
        Guid value) =>
        new(value);
}
