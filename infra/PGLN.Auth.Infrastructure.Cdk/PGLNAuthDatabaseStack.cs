using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Constructs;

namespace PGLN.Auth.Infrastructure.Cdk;

public sealed class PGLNAuthDatabaseStack : Stack
{
    public DatabaseInstance Database { get; }

    public PGLNAuthDatabaseStack(
        Construct scope,
        string id,
        IVpc vpc,
        ISecurityGroup bastionSecurityGroup,
        IStackProps? props = null)
        : base(scope, id, props)
    {
        Amazon.CDK.Tags.Of(this).Add("Project", "PGLN.Auth");
        Amazon.CDK.Tags.Of(this).Add("Environment", "Development");
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");

        const string masterUsername = "pglnadmin";
        const string databaseName = "pgln_auth";

        Database = new DatabaseInstance(this, "AuthDatabase", new DatabaseInstanceProps
        {
            InstanceIdentifier = "pgln-auth-dev-db",

            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
            {
                Version = PostgresEngineVersion.VER_16
            }),

            InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(
                InstanceClass.BURSTABLE4_GRAVITON,
                InstanceSize.MICRO),

            Vpc = vpc,

            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },

            AllocatedStorage = 20,
            StorageType = StorageType.GP3,
            StorageEncrypted = true,

            MultiAz = false,
            PubliclyAccessible = false,
            Port = 5432,

            DatabaseName = databaseName,
            Credentials = Credentials.FromGeneratedSecret(masterUsername),

            BackupRetention = Duration.Days(0),
            DeleteAutomatedBackups = true,

            RemovalPolicy = RemovalPolicy.DESTROY,
            DeletionProtection = false
        });

        Database.Connections.AllowDefaultPortFrom(
            bastionSecurityGroup,
            "Allow SSM bastion to reach PostgreSQL");

        new CfnOutput(this, "DbEndpoint", new CfnOutputProps
        {
            Value = Database.DbInstanceEndpointAddress,
            Description = "PGLN.Auth dev PostgreSQL endpoint"
        });

        new CfnOutput(this, "DbPort", new CfnOutputProps
        {
            Value = Database.DbInstanceEndpointPort,
            Description = "PGLN.Auth dev PostgreSQL port"
        });

        new CfnOutput(this, "DbName", new CfnOutputProps
        {
            Value = databaseName,
            Description = "PGLN.Auth dev PostgreSQL database name"
        });

        new CfnOutput(this, "DbSecretArn", new CfnOutputProps
        {
            Value = Database.Secret!.SecretArn,
            Description = "PGLN.Auth dev DB credentials secret ARN"
        });

        new CfnOutput(this, "DbSecurityGroupId", new CfnOutputProps
        {
            Value = Database.Connections.SecurityGroups[0].SecurityGroupId,
            Description = "PGLN.Auth dev DB security group ID"
        });
    }
}