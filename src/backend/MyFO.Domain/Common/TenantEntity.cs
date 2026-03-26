namespace MyFO.Domain.Common;

public abstract class TenantEntity : BaseEntity
{
    public Guid FamilyId { get; set; }
}
