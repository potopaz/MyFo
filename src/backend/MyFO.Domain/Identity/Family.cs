using MyFO.Domain.Common;

namespace MyFO.Domain.Identity;

/// <summary>
/// The tenant itself. Each family is an isolated tenant in the system.
/// Does NOT inherit from TenantEntity because it IS the tenant.
/// </summary>
public class Family : BaseEntity
{
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Currency configuration
    public string PrimaryCurrencyCode { get; set; } = string.Empty;   // ISO 4217 (e.g. "ARS")
    public string SecondaryCurrencyCode { get; set; } = string.Empty; // ISO 4217 (e.g. "USD")

    // Internationalization
    public string Language { get; set; } = "es";                       // ISO 639-1

    // Navigation
    public ICollection<FamilyMember> Members { get; set; } = [];
}
