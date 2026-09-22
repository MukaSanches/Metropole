namespace Metropole.Sim;

public enum SimulationLodTier
{
    Statistical = 0,
    Regional = 1,
    Active = 2,
    Interactive = 3
}

public sealed class AaaWorldState
{
    public const int CurrentVersion = 1;
    public int Version { get; set; } = CurrentVersion;
    public bool Initialized { get; set; }
    public int LastProcessedDay { get; set; } = -1;
    public int? InteractiveCitizenId { get; set; }
    public Dictionary<int, CitizenSimulationProfile> Citizens { get; set; } = new();
    public Dictionary<int, HouseholdState> Households { get; set; } = new();
    public Dictionary<int, PropertyState> Properties { get; set; } = new();
    public Dictionary<int, VehicleState> Vehicles { get; set; } = new();
    public Dictionary<int, RegionSimulationState> Regions { get; set; } = new();
    public List<TrafficLinkState> TrafficLinks { get; set; } = new();
    public SchedulerState Scheduler { get; set; } = new();
    public PerformanceBudgetState Performance { get; set; } = new();
    public int NextPropertyId { get; set; } = 1;
    public int NextVehicleId { get; set; } = 1;
    public int NextMemoryId { get; set; } = 1;
}

public sealed class CitizenSimulationProfile
{
    public int CitizenId { get; set; }
    public int RegionId { get; set; }
    public SimulationLodTier Lod { get; set; }
    public NeedPressureState Needs { get; set; } = new();
    public PersonalityState Personality { get; set; } = new();
    public List<RoutineSlot> Routine { get; set; } = new();
    public List<CitizenMemoryState> Memories { get; set; } = new();
    public CitizenGoalState CurrentGoal { get; set; } = new();
    public int? LastEmployerCompanyId { get; set; }
    public int LastDistrictId { get; set; }
    public int? LastPartnerCitizenId { get; set; }
    public string LastActivity { get; set; } = "";
}

public sealed class NeedPressureState
{
    public decimal Hunger { get; set; }
    public decimal Thirst { get; set; }
    public decimal Fatigue { get; set; }
    public decimal Hygiene { get; set; }
    public decimal Bathroom { get; set; }
    public decimal Social { get; set; }
    public decimal Fun { get; set; }
    public decimal Comfort { get; set; }
    public decimal Safety { get; set; }
    public decimal Health { get; set; }
}

public sealed class PersonalityState
{
    public decimal Extroversion { get; set; }
    public decimal Responsibility { get; set; }
    public decimal Ambition { get; set; }
    public decimal Sociability { get; set; }
    public decimal Aggressiveness { get; set; }
    public decimal Generosity { get; set; }
    public decimal Romanticism { get; set; }
    public decimal Courage { get; set; }
    public decimal Curiosity { get; set; }
    public decimal Patience { get; set; }
}

public sealed class RoutineSlot
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public string Activity { get; set; } = "";
    public int? TargetDistrictId { get; set; }
}

public sealed class CitizenMemoryState
{
    public int Id { get; set; }
    public int Day { get; set; }
    public string EventType { get; set; } = "";
    public string Summary { get; set; } = "";
    public decimal EmotionalWeight { get; set; }
    public decimal Importance { get; set; }
    public int? RelatedEntityId { get; set; }
}

public sealed class CitizenGoalState
{
    public string Need { get; set; } = "Conforto";
    public string Action { get; set; } = "Ficar em casa";
    public string? PreferredAffordanceId { get; set; }
    public decimal Utility { get; set; }
    public int CreatedDay { get; set; }
}

public sealed class HouseholdState
{
    public int Id { get; set; }
    public int DistrictId { get; set; }
    public int PropertyId { get; set; }
    public List<int> ResidentCitizenIds { get; set; } = new();
}

public sealed class PropertyState
{
    public int Id { get; set; }
    public int DistrictId { get; set; }
    public string Kind { get; set; } = "Apartamento";
    public int? OwnerCitizenId { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal Condition { get; set; } = 0.85m;
    public int Rooms { get; set; } = 3;
    public List<int> ResidentCitizenIds { get; set; } = new();
}

public sealed class VehicleState
{
    public int Id { get; set; }
    public int OwnerCitizenId { get; set; }
    public string Type { get; set; } = "Carro";
    public int CurrentDistrictId { get; set; }
    public int? TargetDistrictId { get; set; }
    public string Status { get; set; } = "Estacionado";
    public decimal Condition { get; set; } = 0.90m;
    public decimal EstimatedValue { get; set; }
}

public sealed class RegionSimulationState
{
    public int DistrictId { get; set; }
    public string Name { get; set; } = "";
    public int Population { get; set; }
    public int StatisticalCitizens { get; set; }
    public int RegionalCitizens { get; set; }
    public int ActiveCitizens { get; set; }
    public int InteractiveCitizens { get; set; }
    public int Companies { get; set; }
    public int Vehicles { get; set; }
    public decimal ActivityIndex { get; set; }
}

public sealed class TrafficLinkState
{
    public int FromDistrictId { get; set; }
    public int ToDistrictId { get; set; }
    public int Capacity { get; set; } = 180;
    public int DailyDemand { get; set; }
    public decimal Congestion { get; set; }
}

public sealed class SchedulerState
{
    public int Cursor { get; set; }
    public int BatchSize { get; set; } = 128;
    public long TotalHourlyTicks { get; set; }
    public int LastWorkUnits { get; set; }
}

public sealed class PerformanceBudgetState
{
    public int TargetFps { get; set; } = 60;
    public int ActiveCitizenCap { get; set; } = 96;
    public int InteractiveCitizenCap { get; set; } = 1;
    public int VisualCitizenBudgetLow { get; set; } = 16;
    public int VisualCitizenBudgetMedium { get; set; } = 32;
    public int VisualCitizenBudgetHigh { get; set; } = 56;
    public int VisualCitizenBudgetUltra { get; set; } = 80;
}

public sealed record InteractionAffordanceDefinition(
    string Id,
    string Provider,
    string Label,
    int DurationHours,
    string SatisfiesNeed,
    decimal NeedRelief,
    decimal Cost,
    string AnimationHint);

public sealed record AaaSimulationSnapshot(
    int Citizens,
    int Statistical,
    int Regional,
    int Active,
    int Interactive,
    int Regions,
    int Households,
    int Properties,
    int Vehicles,
    int TrafficLinks,
    decimal AverageCongestion,
    long SchedulerTicks,
    int SchedulerBatchSize);

public sealed record AaaScaleProbeResult(
    int Population,
    int Statistical,
    int Regional,
    int Active,
    int Interactive);
