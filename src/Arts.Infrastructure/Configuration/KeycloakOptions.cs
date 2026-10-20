namespace Arts.Infrastructure.Configuration;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;

    public string RealmUrl => $"{BaseUrl}/realms/{Realm}";
}