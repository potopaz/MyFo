using MyFO.Domain.Common;
using MyFO.Domain.Identity;

namespace MyFO.Domain.Transactions;

public class CashBoxPermission : TenantEntity
{
    public Guid CashBoxId { get; set; }
    public Guid MemberId { get; set; }

    public CashBox CashBox { get; set; } = null!;
    public FamilyMember Member { get; set; } = null!;
}
