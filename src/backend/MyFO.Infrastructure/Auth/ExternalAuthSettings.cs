namespace MyFO.Infrastructure.Auth;

public class ExternalAuthSettings
{
    public ExternalProvider Google { get; set; } = new();
    public ExternalProvider Microsoft { get; set; } = new();
    public ExternalProvider Apple { get; set; } = new();
}

public class ExternalProvider
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
