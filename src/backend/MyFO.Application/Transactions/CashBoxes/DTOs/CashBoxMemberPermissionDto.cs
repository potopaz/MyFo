namespace MyFO.Application.Transactions.CashBoxes.DTOs;

public class CashBoxMemberPermissionDto
{
    public Guid MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Permission { get; set; } // null = no access, "Operate" = can operate
}
