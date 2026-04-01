namespace MyFO.Application.Common;

/// <summary>
/// Database scalar functions mapped via EF Core HasDbFunction.
/// These methods must ONLY be called inside EF LINQ expressions.
/// Registration happens in ApplicationDbContext.OnModelCreating.
/// </summary>
public static class PgFunctions
{
    /// <summary>
    /// Maps to PostgreSQL unaccent() — removes diacritics/accents from text.
    /// </summary>
    public static string Unaccent(string input) => throw new NotSupportedException("Call via EF LINQ only");
}
