namespace MyEngine.Utils;

public static class EnumerableExtensions
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
        where T : class
    {
        return (IEnumerable<T>)enumerable.Where(x => x is not null);
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
        where T : struct
    {
        return enumerable
            .Where(x => x.HasValue)
            .Select(x => x!.Value);
    }
}
