using MediatR;
using MyFO.Application.Transactions.BankAccounts.DTOs;

namespace MyFO.Application.Transactions.BankAccounts.Commands;

public class CreateBankAccountCommand : IRequest<BankAccountDto>
{
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public string? AccountNumber { get; set; }
    public string? Cbu { get; set; }
    public string? Alias { get; set; }
}
