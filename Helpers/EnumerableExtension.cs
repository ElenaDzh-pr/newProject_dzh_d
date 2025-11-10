namespace ProjectDz.Helpers;

public static class EnumerableExtension
{
    public static IEnumerable<T> GetBatchByNumber<T>(this IEnumerable<T> source, int batchSize, int batchNumber)
    {
        return source.Skip(batchNumber * batchSize).Take(batchSize);
    }
    
}