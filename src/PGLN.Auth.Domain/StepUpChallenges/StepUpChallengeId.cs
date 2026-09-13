namespace PGLN.Auth.Domain.StepUpChallenges;

public readonly record struct StepUpChallengeId(Guid Value)
{
    public static StepUpChallengeId New()
    {
        return new StepUpChallengeId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
