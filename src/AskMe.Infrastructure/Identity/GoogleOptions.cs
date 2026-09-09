namespace AskMe.Infrastructure.Identity;

public class GoogleOptions
{
    public const string SectionName = "Google";

    // OAuth Client ID from Google Cloud Console (Credentials > OAuth 2.0
    // Client IDs, type "Web application"). Used to verify the audience claim
    // on ID tokens issued to the frontend's Google Identity Services button.
    public required string ClientId { get; set; }
}
