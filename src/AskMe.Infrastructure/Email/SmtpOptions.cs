namespace AskMe.Infrastructure.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; set; }
    public int Port { get; set; } = 587;
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FromAddress { get; set; }
    public string FromName { get; set; } = "Ask Me";
}
