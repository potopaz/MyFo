using MyFO.Domain.Accounting.Enums;
using MyFO.Domain.Common;

namespace MyFO.Domain.Accounting;

/// <summary>
/// A subcategory within a category.
///
/// This is where the real classification happens:
///   - SubcategoryType: Income/Expense/Both (filters which subcategories show)
///   - AccountingType: Asset/Liability/Income/Expense (for accounting reports)
///   - SuggestedCostCenterId: pre-fills cost center when selected
///   - IsOrdinary: ordinary vs extraordinary (for reporting)
///
/// AccountingType, SuggestedCostCenterId, and IsOrdinary are SUGGESTIONS
/// that pre-fill the movement form but can be changed by the user.
/// SubcategoryType is FIXED and used for filtering.
/// </summary>
public class Subcategory : TenantEntity
{
    public Guid SubcategoryId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Fixed: filters which subcategories appear for income vs expense
    public SubcategoryType SubcategoryType { get; set; }

    public bool IsActive { get; set; } = true;

    // Suggestions (nullable = user must choose at movement level)
    public AccountingType? SuggestedAccountingType { get; set; }
    public Guid? SuggestedCostCenterId { get; set; }
    public bool? IsOrdinary { get; set; }

    public Category Category { get; set; } = null!;
    public CostCenter? SuggestedCostCenter { get; set; }
}
