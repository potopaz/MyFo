namespace MyFO.Application.Transactions.CashBoxes.DTOs;

public class CashBoxDto
{
    public Guid CashBoxId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public bool CanOperate { get; set; }
}
