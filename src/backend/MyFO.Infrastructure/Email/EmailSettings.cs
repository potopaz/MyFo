namespace MyFO.Infrastructure.Email;

public class EmailSettings
{
    public string SenderEmail { get; set; } = "onboarding@resend.dev";
    public string SenderName { get; set; } = "MyFO";
    public string ResendApiKey { get; set; } = string.Empty;
}
