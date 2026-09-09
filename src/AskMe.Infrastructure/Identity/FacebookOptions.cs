namespace AskMe.Infrastructure.Identity;

public class FacebookOptions
{
    public const string SectionName = "Facebook";

    // From the Facebook Developer app (developers.facebook.com > your app > Settings > Basic).
    public required string AppId { get; set; }
    public required string AppSecret { get; set; }
}
