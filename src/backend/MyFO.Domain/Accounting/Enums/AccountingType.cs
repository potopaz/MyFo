namespace MyFO.Domain.Accounting.Enums;

/// <summary>
/// Accounting classification for reporting.
/// Allows generating balance sheets and income statements.
///
/// Asset: comprar un auto, una propiedad
/// Liability: tomar un préstamo, deuda
/// Income: salario, venta
/// Expense: compra de consumo, servicios
/// </summary>
public enum AccountingType
{
    Asset = 0,
    Liability = 1,
    Income = 2,
    Expense = 3
}
