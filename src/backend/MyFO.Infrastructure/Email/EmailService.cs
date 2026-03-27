using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFO.Application.Common.Interfaces;

namespace MyFO.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, IHttpClientFactory httpClientFactory, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("Resend");

        var payload = new
        {
            from = $"{_settings.SenderName} <{_settings.SenderEmail}>",
            to = new[] { to },
            subject,
            html = htmlBody,
        };

        var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Email sent to {To} with subject '{Subject}'", to, subject);
    }
}
