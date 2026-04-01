using MyFO.Application.Common.Mediator;
using MyFO.Application.Transactions.BankAccounts.DTOs;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public class UpdateBankAccountCommand : IRequest<BankAccountDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid BankAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public string? Cbu { get; set; }
    public string? Alias { get; set; }
    public bool IsActive { get; set; } = true;
}
