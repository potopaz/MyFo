using MyFO.Domain.Common;

namespace MyFO.Domain.Accounting;

/// <summary>
/// Association between a family and the currencies they use.
///
/// When a family registers, their primary (and optional secondary) currency
/// is automatically associated. They can add more currencies later.
///
/// A currency cannot be deactivated if it's being used by cash boxes,
/// bank accounts, or movements.
/// </summary>
public class FamilyCurrency : TenantEntity
{
    public Guid FamilyCurrencyId { get; set; }
    public Guid CurrencyId { get; set; }
    public bool IsActive { get; set; } = true;

    public Currency Currency { get; set; } = null!;
}
