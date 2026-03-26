namespace MyFO.Application.Auth.DTOs;

public class CheckEmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public class CheckEmailResponse
{
    public bool Exists { get; set; }
}
