using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.Tests.Users;

public sealed class EmailTests
{
    [Fact]
    public void Create_Should_Normalize_Email()
    {
        var email = Email.Create("User@Example.com");

        Assert.Equal("User@Example.com", email.Value);
        Assert.Equal("USER@EXAMPLE.COM", email.NormalizedValue);
    }

    [Fact]
    public void Create_Should_Trim_Email()
    {
        var email = Email.Create("  user@example.com  ");

        Assert.Equal("user@example.com", email.Value);
    }

    [Fact]
    public void Create_Should_Reject_Invalid_Email()
    {
        Assert.Throws<ArgumentException>(() =>
            Email.Create("not-an-email"));
    }
}