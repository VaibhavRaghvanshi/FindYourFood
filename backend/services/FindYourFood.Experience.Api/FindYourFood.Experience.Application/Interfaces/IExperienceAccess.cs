namespace FindYourFood.Experience.Application.Interfaces;

public static class ExperiencePermissions
{
    public const string Scope = "/experience/customer-home";
    public const string Edit = "experience.edit";
    public const string Publish = "experience.publish";
}

public enum AccessDecision { Allowed, Denied, Unavailable }

public interface IExperienceAccess
{
    Task<AccessDecision> CheckAsync(string permission, string bearerToken, CancellationToken cancellationToken);
}
