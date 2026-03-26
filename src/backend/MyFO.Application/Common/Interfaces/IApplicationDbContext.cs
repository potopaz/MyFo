using Microsoft.EntityFrameworkCore;
using MyFO.Domain.Accounting;
using MyFO.Domain.Common;
using MyFO.Domain.CreditCards;
using MyFO.Domain.Identity;
using MyFO.Domain.Transactions;

namespace MyFO.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the DbContext for use in Application layer handlers.
///
/// This lets handlers do complex queries (Include, joins, projections)
/// without depending on the Infrastructure layer directly.
///
/// The generic IRepository is good for simple CRUD, but when you need
/// Include() or complex LINQ, this interface gives you direct DbSet access
/// while still keeping the dependency inverted.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Family> Families { get; }
    DbSet<FamilyMember> FamilyMembers { get; }
    DbSet<FamilyAdminConfig> FamilyAdminConfigs { get; }
    DbSet<FamilyInvitation> FamilyInvitations { get; }
    DbSet<Currency> Currencies { get; }
    DbSet<FamilyCurrency> FamilyCurrencies { get; }
    DbSet<Category> Categories { get; }
    DbSet<Subcategory> Subcategories { get; }
    DbSet<CostCenter> CostCenters { get; }
    DbSet<CashBox> CashBoxes { get; }
    DbSet<CashBoxPermission> CashBoxPermissions { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<CreditCard> CreditCards { get; }
    DbSet<CreditCardMember> CreditCardMembers { get; }
    DbSet<CreditCardInstallment> CreditCardInstallments { get; }
    DbSet<StatementPeriod> StatementPeriods { get; }
    DbSet<StatementLineItem> StatementLineItems { get; }

    DbSet<StatementPaymentAllocation> StatementPaymentAllocations { get; }
    DbSet<CreditCardPayment> CreditCardPayments { get; }
    DbSet<Movement> Movements { get; }
    DbSet<MovementPayment> MovementPayments { get; }
    DbSet<FrequentMovement> FrequentMovements { get; }
    DbSet<Transfer> Transfers { get; }
    DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
