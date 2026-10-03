using Amazon.CDK;

namespace PGLN.Auth.Infrastructure.Cdk;

internal static class Program
{
    private static void Main(string[] args)
    {
        var app = new App();

        var account = app.Node.TryGetContext("account")?.ToString();

        var region = app.Node.TryGetContext("region")?.ToString()
            ?? "af-south-1";

        if (string.IsNullOrWhiteSpace(account))
        {
            throw new InvalidOperationException(
                "AWS account is required. Pass -c account=<your-account-id>.");
        }

        var environment = new Amazon.CDK.Environment
        {
            Account = account,
            Region = region
        };

        var networkStack = new PGLNAuthStack(
            app,
            "PGLNAuthDev",
            new StackProps
            {
                Env = environment,
                Description = "PGLN.Auth development networking infrastructure"
            });

        var bastionStack = new PGLNAuthBastionStack(
            app,
            "PGLNAuthDevBastion",
            networkStack.Vpc,
            new StackProps
            {
                Env = environment,
                Description = "PGLN.Auth development SSM bastion host"
            });

        new PGLNAuthDatabaseStack(
            app,
            "PGLNAuthDevDatabase",
            networkStack.Vpc,
            bastionStack.BastionSecurityGroup,
            new StackProps
            {
                Env = environment,
                Description = "PGLN.Auth development PostgreSQL database"
            });

        app.Synth();
    }
}