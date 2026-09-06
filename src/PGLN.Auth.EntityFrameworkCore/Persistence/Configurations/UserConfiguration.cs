using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

public sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        var userIdConverter =
            new ValueConverter<UserId, Guid>(
                id => id.Value,
                value => new UserId(value));

        var emailConverter =
            new ValueConverter<Email, string>(
                email => email.Value,
                value => Email.Create(value));

        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder
            .Property(user => user.Id)
            .HasConversion(userIdConverter)
            .ValueGeneratedNever();

        builder
            .Property(user => user.Email)
            .HasConversion(emailConverter)
            .HasMaxLength(320)
            .IsRequired();

        builder
            .Property(user => user.NormalizedEmail)
            .HasField("_normalizedEmail")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field)
            .HasMaxLength(320)
            .IsRequired();

        builder
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        builder
            .Property(user => user.PasswordHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .Property(user => user.EmailConfirmed)
            .IsRequired();

        builder
            .Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder
            .Property(user => user.EmailConfirmedAtUtc);

        builder.Ignore(
            user => user.DomainEvents);
    }
}
