using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyFO.Application.Common.Interfaces;
using MyFO.Domain.Accounting;
using MyFO.Domain.Common;
using MyFO.Domain.CreditCards;
using MyFO.Domain.Identity;
using MyFO.Domain.Interfaces.Services;
using MyFO.Domain.Transactions;
using MyFO.Infrastructure.Identity;

namespace MyFO.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    // ----- public: ASP.NET Identity (configured by base class) -----

    // ----- cmn: Global tables (no tenant) -----
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRateSnapshot> ExchangeRateSnapshots => Set<ExchangeRateSnapshot>();

    // ----- cfg: Configuration / tenant setup -----
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<FamilyAdminConfig> FamilyAdminConfigs => Set<FamilyAdminConfig>();
    public DbSet<FamilyInvitation> FamilyInvitations => Set<FamilyInvitation>();
    public DbSet<FamilyCurrency> FamilyCurrencies => Set<FamilyCurrency>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Subcategory> Subcategories => Set<Subcategory>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<CashBox> CashBoxes => Set<CashBox>();
    public DbSet<CashBoxPermission> CashBoxPermissions => Set<CashBoxPermission>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CreditCardMember> CreditCardMembers => Set<CreditCardMember>();

    // ----- txn: Transactions / operations -----
    public DbSet<Movement> Movements => Set<Movement>();
    public DbSet<MovementPayment> MovementPayments => Set<MovementPayment>();
    public DbSet<FrequentMovement> FrequentMovements => Set<FrequentMovement>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<CreditCardInstallment> CreditCardInstallments => Set<CreditCardInstallment>();
    public DbSet<CreditCardPayment> CreditCardPayments => Set<CreditCardPayment>();
    public DbSet<StatementPeriod> StatementPeriods => Set<StatementPeriod>();
    public DbSet<StatementLineItem> StatementLineItems => Set<StatementLineItem>();
    public DbSet<StatementPaymentAllocation> StatementPaymentAllocations => Set<StatementPaymentAllocation>();

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Ensure custom schemas exist
        builder.HasDefaultSchema("public");

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyGlobalFilters(builder);
    }

    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(TenantEntity).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType).HasQueryFilter(BuildTenantFilter(clrType));
            }
            else if (typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType).HasQueryFilter(BuildSoftDeleteFilter(clrType));
            }
        }
    }

    private static System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var deletedAtProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
        var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?));
        var isNotDeleted = System.Linq.Expressions.Expression.Equal(deletedAtProperty, nullConstant);
        return System.Linq.Expressions.Expression.Lambda(isNotDeleted, parameter);
    }

    private System.Linq.Expressions.LambdaExpression BuildTenantFilter(Type entityType)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "e");

        // e.DeletedAt == null
        var deletedAtProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
        var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?));
        var notDeleted = System.Linq.Expressions.Expression.Equal(deletedAtProperty, nullConstant);

        // e.FamilyId == (Guid?)_currentUser.FamilyId
        var familyIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(TenantEntity.FamilyId));
        var familyIdAsNullable = System.Linq.Expressions.Expression.Convert(familyIdProperty, typeof(Guid?));
        var currentUserExpr = System.Linq.Expressions.Expression.Constant(this);
        var currentUserServiceExpr = System.Linq.Expressions.Expression.Field(currentUserExpr, "_currentUser");
        var currentFamilyIdExpr = System.Linq.Expressions.Expression.Property(currentUserServiceExpr, nameof(ICurrentUserService.FamilyId));
        var familyEquals = System.Linq.Expressions.Expression.Equal(familyIdAsNullable, currentFamilyIdExpr);

        var combined = System.Linq.Expressions.Expression.AndAlso(notDeleted, familyEquals);
        return System.Linq.Expressions.Expression.Lambda(combined, parameter);
    }
}
