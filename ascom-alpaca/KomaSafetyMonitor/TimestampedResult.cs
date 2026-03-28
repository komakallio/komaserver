namespace KomaSafetyMonitor;

public record TimestampedResult<T>(T? Value, DateTime FetchedAt);
