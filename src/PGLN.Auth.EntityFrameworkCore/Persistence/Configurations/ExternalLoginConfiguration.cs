using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PGLN.Auth.Domain.ExternalLogins;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Configurations;

internal sealed class ExternalLoginConfiguration
    : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(
        EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");

        builder.HasKey(
            externalLogin =>
                externalLogin.Id);

        builder.Property(
                externalLogin =>
                    externalLogin.Id)
            .HasConversion(
                id => id.Value,
                value =>
                    new ExternalLoginId(value));

        builder.Property(
                externalLogin =>
                    externalLogin.UserId)
            .HasConversion(
                id => id.Value,
                value =>
                    new(value));

        builder.Property(
                externalLogin =>
                    externalLogin.Provider)
            .HasConversion<int>();

        builder.Property(
                externalLogin =>
                    externalLogin.ProviderSubject)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(
                externalLogin =>
                    externalLogin.Email)
            .HasMaxLength(320);

        builder.Property(
                externalLogin =>
                    externalLogin.CreatedAtUtc)
            .IsRequired();

        builder.Property(
                externalLogin =>
                    externalLogin.LastLoginAtUtc)
            .IsRequired();

        builder.HasIndex(
                externalLogin =>
                    new
                    {
                        externalLogin.Provider,
                        externalLogin.ProviderSubject
                    })
            .IsUnique();

        builder.HasIndex(
            externalLogin =>
                externalLogin.UserId);

        builder.HasOne<PGLN.Auth.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(
                externalLogin =>
                    externalLogin.UserId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}
