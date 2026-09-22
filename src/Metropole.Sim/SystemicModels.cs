namespace Metropole.Sim;

public enum PopulationDetailLevel
{
    Abstract = 0,
    Regional = 1,
    Active = 2,
    Interactive = 3
}

public sealed class SystemicSimulationState
{
    public int SchedulerCursor { get; set; }
    public long TotalCitizenDecisions { get; set; }
    public long TotalSocialInteractions { get; set; }
    public int MaxInteractiveCitizens { get; set; } = 24;
    public int MaxActiveCitizens { get; set; } = 96;
    public int LastReclassificationDay { get; set; } = -1;
}

public sealed class SocialRelationState
{
    public int CitizenAId { get; set; }
    public int CitizenBId { get; set; }
    public decimal Familiarity { get; set; }
    public decimal Friendship { get; set; }
    public decimal Trust { get; set; }
    public decimal Attraction { get; set; }
    public decimal Respect { get; set; }
    public decimal Resentment { get; set; }
    public decimal Romance { get; set; }
    public int LastInteractionDay { get; set; }
}

public sealed class CitizenMemoryState
{
    public int CitizenId { get; set; }
    public int Day { get; set; }
    public string Kind { get; set; } = "";
    public string Summary { get; set; } = "";
    public decimal EmotionalWeight { get; set; }
    public decimal Importance { get; set; }
}

public sealed class ResidenceState
{
    public int HouseholdId { get; set; }
    public int DistrictId { get; set; }
    public int Capacity { get; set; } = 4;
    public decimal Quality { get; set; } = 0.60m;
    public decimal MonthlyRent { get; set; }
    public List<int> ResidentIds { get; set; } = [];
}

public sealed record SystemicSnapshot(
    int Population,
    int Interactive,
    int Active,
    int Regional,
    int Abstract,
    int Relationships,
    int Memories,
    int Residences,
    decimal AverageHygiene,
    decimal AverageSocial,
    decimal AverageFun);

public sealed record AffordanceDefinition(
    string Id,
    string Label,
    string Category,
    int DurationHours,
    string EffectSummary);

public sealed partial class GameState
{
    public SystemicSimulationState Systemic { get; set; } = new();
    public List<SocialRelationState> Relationships { get; set; } = [];
    public List<CitizenMemoryState> Memories { get; set; } = [];
    public List<ResidenceState> Residences { get; set; } = [];
}

public sealed partial class PlayerState
{
    public decimal Hygiene { get; set; } = 78m;
    public decimal Fun { get; set; } = 62m;
    public decimal Comfort { get; set; } = 68m;
    public decimal Security { get; set; } = 72m;
    public string CurrentGoal { get; set; } = "Manter uma rotina equilibrada";
}

public sealed partial class CitizenState
{
    public decimal Health { get; set; } = 90m;
    public decimal Hygiene { get; set; } = 72m;
    public decimal SocialNeed { get; set; } = 58m;
    public decimal Fun { get; set; } = 55m;
    public decimal Comfort { get; set; } = 62m;
    public string CurrentGoal { get; set; } = "Manter rotina";
    public PopulationDetailLevel SimulationDetail { get; set; } = PopulationDetailLevel.Abstract;
}
