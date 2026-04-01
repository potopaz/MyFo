using MyFO.Application.Common.Mediator;
using MyFO.Application.Accounting.FamilyCurrencies.DTOs;

namespace MyFO.Application.Accounting.FamilyCurrencies.Commands;

public class AddFamilyCurrencyCommand : IRequest<FamilyCurrencyDto>
{
    public string CurrencyCode { get; set; } = string.Empty;
}
