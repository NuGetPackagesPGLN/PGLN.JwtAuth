using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Registration;

public static class RegistrationErrors
{
    public static readonly Error EmailAlreadyExists =
        new(
            "Registration.EmailAlreadyExists",
            "A user with this email address already exists.");
}