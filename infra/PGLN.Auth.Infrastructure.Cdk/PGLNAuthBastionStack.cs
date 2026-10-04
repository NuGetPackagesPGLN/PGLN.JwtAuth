using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Constructs;

using Ec2Instance = Amazon.CDK.AWS.EC2.Instance_;

namespace PGLN.Auth.Infrastructure.Cdk;

public sealed class PGLNAuthBastionStack : Stack
{
    public ISecurityGroup BastionSecurityGroup { get; }

    public PGLNAuthBastionStack(
        Construct scope,
        string id,
        IVpc vpc,
        IStackProps? props = null)
        : base(scope, id, props)
    {
        Amazon.CDK.Tags.Of(this).Add("Project", "PGLN.Auth");
        Amazon.CDK.Tags.Of(this).Add("Environment", "Development");
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");

        BastionSecurityGroup = new SecurityGroup(this, "BastionSecurityGroup", new SecurityGroupProps
        {
            Vpc = vpc,
            Description = "PGLN.Auth dev SSM bastion - outbound only",
            AllowAllOutbound = true
        });

        var role = new Role(this, "BastionRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
            Description = "PGLN.Auth dev SSM bastion instance role",
            ManagedPolicies = new[]
            {
                ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore")
            }
        });

        var instance = new Ec2Instance(this, "BastionInstance", new InstanceProps
        {
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(
                InstanceClass.BURSTABLE4_GRAVITON,
                InstanceSize.MICRO),
            MachineImage = MachineImage.LatestAmazonLinux2023(
                new AmazonLinux2023ImageSsmParameterProps
                {
                    CpuType = AmazonLinuxCpuType.ARM_64
                }),
            Role = role,
            SecurityGroup = BastionSecurityGroup,
            RequireImdsv2 = true
        });

        new CfnOutput(this, "BastionInstanceId", new CfnOutputProps
        {
            Value = instance.InstanceId,
            Description = "SSM bastion instance ID for RDS port-forwarding"
        });
    }
}
