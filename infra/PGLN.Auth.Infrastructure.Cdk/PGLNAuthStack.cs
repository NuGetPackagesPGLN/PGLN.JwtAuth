using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace PGLN.Auth.Infrastructure.Cdk;

public sealed class PGLNAuthStack : Stack
{
    public IVpc Vpc { get; }

    public PGLNAuthStack(
        Construct scope,
        string id,
        IStackProps? props = null)
        : base(scope, id, props)
    {
        Amazon.CDK.Tags.Of(this).Add("Project", "PGLN.Auth");
        Amazon.CDK.Tags.Of(this).Add("Environment", "Development");
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");

        Vpc = new Vpc(this, "AuthVpc", new VpcProps
        {
            VpcName = "pgln-auth-dev-vpc",
            IpAddresses = IpAddresses.Cidr("10.40.0.0/16"),
            AvailabilityZones = new[]
            {
                "af-south-1a",
                "af-south-1b"
            },
            NatGateways = 0,

            SubnetConfiguration = new[]
            {
                new SubnetConfiguration
                {
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC,
                    CidrMask = 24
                },
                new SubnetConfiguration
                {
                    Name = "Database",
                    SubnetType = SubnetType.PRIVATE_ISOLATED,
                    CidrMask = 24
                }
            }
        });

        new CfnOutput(this, "VpcId", new CfnOutputProps
        {
            Value = Vpc.VpcId,
            Description = "PGLN.Auth development VPC ID"
        });
    }
}