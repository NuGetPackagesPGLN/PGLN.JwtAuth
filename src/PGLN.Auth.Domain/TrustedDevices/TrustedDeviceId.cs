namespace PGLN.Auth.Domain.TrustedDevices;

public readonly record struct TrustedDeviceId(Guid Value)
{
    public static TrustedDeviceId New()
    {
        return new TrustedDeviceId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
