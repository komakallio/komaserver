namespace KomaSafetyMonitor.SafetyRestApi;

public record SafetyStatus
{
    public required bool Safe { get; init; }
    public required SafetyStatusDetails Details { get; init; }

    public record SafetyStatusDetails
    {
        public required SafetyValue Temperature { get; init; }
        public required SafetyValue RainIntensity { get; init; }
        public required SafetyValue RainTrigger { get; init; }
        public required SafetyValue RainRadar10Km { get; init; }
        public required SafetyValue RainRadar30Km { get; init; }
        public required SafetyValue RainRadar50Km { get; init; }
        public required SafetyValue SunAltitude { get; init; }
        public required SafetyValue MoonAltitude { get; init; }
        public required SafetyValue UpsCharge { get; init; }
        public required SafetyValue EnclosureTemp { get; init; }
    }

    public record SafetyValue
    {
        public required double Value { get; init; }
        public required bool Safe { get; init; }
    }
}
