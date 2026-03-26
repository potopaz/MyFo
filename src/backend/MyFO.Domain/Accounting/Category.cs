using MyFO.Domain.Common;

namespace MyFO.Domain.Accounting;

/// <summary>
/// A category for grouping subcategories.
///
/// Categories are just organizational containers. The real classification
/// (income/expense, accounting type, etc.) lives on the Subcategory.
///
/// Examples: "Hogar", "Salarios", "Transporte", "Impuestos"
/// </summary>
public class Category : TenantEntity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = [];
}
