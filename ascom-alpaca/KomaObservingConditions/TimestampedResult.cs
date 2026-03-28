namespace KomaObservingConditions;

public record TimestampedResult<T>(T? Value, DateTime FetchedAt);
