namespace Metropole.Sim;

public sealed class GameState
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string RulesVersion { get; set; } = "1.5.0";
    public long Seed { get; set; }
    public int CurrentDay { get; set; }
    public int CurrentHour { get; set; } = 8;
    public string Weather { get; set; } = "Ensolarado";
    public decimal TemperatureC { get; set; } = 24m;
    public decimal CityConfidence { get; set; } = 0.55m;
    public decimal Treasury { get; set; }
    public PlayerState Player { get; set; } = new();
    public List<DistrictState> Districts { get; set; } = [];
    public List<CitizenState> Citizens { get; set; } = [];
    public List<CompanyState> Companies { get; set; } = [];
    public List<ProductMarketState> Markets { get; set; } = [];
    public List<HistoryEvent> History { get; set; } = [];
    public int NextCitizenId { get; set; } = 1;
    public int NextCompanyId { get; set; } = 1;

    public decimal TotalLiquidMoney() =>
        Treasury + Player.Cash + Citizens.Sum(x => x.Cash) + Companies.Sum(x => x.Cash);

    public int Population => Citizens.Count(x => x.Alive);
    public int OpenCompanies => Companies.Count(x => x.Open);
    public int EmployedCitizens => Citizens.Count(x => x.Alive && x.EmployedCompanyId is not null);
    public decimal UnemploymentRate
    {
        get
        {
            var workingAge = Citizens.Count(x => x.Alive && x.AgeYears is >= 18 and < 66);
            if (workingAge == 0) return 0m;
            return 1m - (decimal)EmployedCitizens / workingAge;
        }
    }
}

public sealed class PlayerState
{
    public string Name { get; set; } = "Cidadão";
    public int Generation { get; set; } = 1;
    public int AgeDays { get; set; } = 21 * 365;
    public decimal Cash { get; set; } = 2_500m;
    public int DistrictId { get; set; } = 5;
    public int? EmployerCompanyId { get; set; }
    public decimal DailyWage { get; set; }
    public int? BusinessCompanyId { get; set; }
    public decimal Hunger { get; set; } = 15m;
    public decimal Energy { get; set; } = 100m;
    public decimal Health { get; set; } = 92m;
    public decimal Stress { get; set; } = 18m;
    public decimal Happiness { get; set; } = 64m;
    public decimal Social { get; set; } = 58m;
    public decimal Fitness { get; set; } = 45m;
    public decimal CareerReputation { get; set; } = 0.35m;
    public decimal EducationProgress { get; set; }
    public int EducationLevel { get; set; } = 2;
    public int CareerExperienceDays { get; set; }
    public decimal WorkedHoursToday { get; set; }
    public string CurrentActivity { get; set; } = "Em casa";
    public string RelationshipStatus { get; set; } = "Solteiro";
    public string? PartnerName { get; set; }
    public int? PartnerCitizenId { get; set; }
    public int RelationshipStartDay { get; set; } = -1;
    public int MarriageDay { get; set; } = -1;
    public int Children { get; set; }
    public Dictionary<string, int> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Comunicação"] = 1,
        ["Organização"] = 1,
        ["Raciocínio"] = 1
    };
    public int AgeYears => AgeDays / 365;
}

public sealed class DistrictState
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int GridX { get; set; }
    public int GridY { get; set; }
    public decimal RentIndex { get; set; } = 1m;
    public decimal WealthIndex { get; set; } = 1m;
    public decimal LogisticsIndex { get; set; } = 1m;
    public decimal SocialIndex { get; set; } = 1m;
    public decimal SafetyIndex { get; set; } = 1m;
}

public sealed class CitizenState
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int AgeDays { get; set; }
    public decimal Cash { get; set; }
    public int DistrictId { get; set; }
    public int HouseholdId { get; set; }
    public int SkillTier { get; set; } = 1;
    public int EducationLevel { get; set; } = 1;
    public int? EmployedCompanyId { get; set; }
    public decimal DailyWage { get; set; }
    public decimal Hunger { get; set; } = 10m;
    public decimal Energy { get; set; } = 75m;
    public decimal Happiness { get; set; } = 55m;
    public decimal Stress { get; set; } = 20m;
    public decimal Ambition { get; set; } = 0.5m;
    public decimal Sociability { get; set; } = 0.5m;
    public decimal Discipline { get; set; } = 0.5m;
    public decimal RiskTolerance { get; set; } = 0.5m;
    public string CurrentActivity { get; set; } = "Em casa";
    public int? PartnerCitizenId { get; set; }
    public decimal PlayerFamiliarity { get; set; }
    public decimal PlayerAffinity { get; set; }
    public decimal PlayerTrust { get; set; }
    public string PlayerRelationshipStatus { get; set; } = "Desconhecido";
    public bool IsPlayerPartner { get; set; }
    public int LastPlayerInteractionDay { get; set; } = -9999;
    public int LastCareerChangeDay { get; set; }
    public bool Alive { get; set; } = true;
    public int AgeYears => AgeDays / 365;
}

public sealed class CompanyState
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string Slogan { get; set; } = "";
    public string Sector { get; set; } = "";
    public string ProductFamily { get; set; } = "";
    public string Archetype { get; set; } = "";
    public string Strategy { get; set; } = "Equilibrada";
    public string OperatingStatus { get; set; } = "Ativa";
    public int DistrictId { get; set; }
    public int AgeDays { get; set; }
    public decimal Cash { get; set; }
    public decimal Debt { get; set; }
    public decimal BaseWage { get; set; }
    public decimal Reputation { get; set; } = 0.5m;
    public decimal BrandAwareness { get; set; } = 0.15m;
    public decimal CustomerLoyalty { get; set; } = 0.20m;
    public decimal ProductQuality { get; set; } = 0.50m;
    public decimal Innovation { get; set; } = 0.35m;
    public decimal EmployeeMorale { get; set; } = 0.62m;
    public decimal Productivity { get; set; } = 1m;
    public decimal PriceMultiplier { get; set; } = 1m;
    public decimal MarketingBudgetDaily { get; set; } = 20m;
    public decimal MarketShare { get; set; }
    public decimal RivalryIntensity { get; set; }
    public int? RivalCompanyId { get; set; }
    public int DesiredEmployees { get; set; } = 5;
    public List<int> EmployeeIds { get; set; } = [];
    public decimal LastRevenue { get; set; }
    public decimal LastCosts { get; set; }
    public decimal LastPayroll { get; set; }
    public decimal LastMarketing { get; set; }
    public decimal LastRent { get; set; }
    public decimal LastOperations { get; set; }
    public decimal LastTaxes { get; set; }
    public int LossDays { get; set; }
    public bool Open { get; set; } = true;
    public bool PlayerOwned { get; set; }
    public string? ClosureCause { get; set; }
    public List<CompanyFinanceSnapshot> FinanceHistory { get; set; } = [];
}

public sealed class CompanyFinanceSnapshot
{
    public int Day { get; set; }
    public decimal Revenue { get; set; }
    public decimal Costs { get; set; }
    public decimal Profit { get; set; }
    public decimal Cash { get; set; }
    public decimal Debt { get; set; }
    public decimal MarketShare { get; set; }
}

public sealed class ProductMarketState
{
    public string Family { get; set; } = "";
    public decimal BasePrice { get; set; }
    public decimal Price { get; set; }
    public decimal Stock { get; set; }
    public decimal DailyDemand { get; set; }
    public decimal DailySupply { get; set; }
}

public sealed class HistoryEvent
{
    public int Day { get; set; }
    public string Kind { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Cause { get; set; } = "";
    public int? EntityId { get; set; }
}

public sealed record SectorDefinition(string Name, string ProductFamily, decimal BasePrice, decimal Productivity);
public sealed record ProfessionDefinition(string Occupation, string Specialization, string Sector, decimal WageFactor);
public sealed record BusinessArchetype(string Sector, string Format);
public sealed record ProductDefinition(string Name, string Family, string Material, string Grade);
public sealed record ResourceDefinition(string Material, string Form);
public sealed record BuildingDefinition(string Use, string Class);
public sealed record SkillDefinition(string Domain, string Competency);
public sealed record EventDefinition(string Cause, string Intensity, string Scope);
public sealed record TechnologyDefinition(string Area, int Tier);

public sealed record ContentMetrics(
    int ProfessionArchetypes,
    int CareerCombinations,
    int BusinessArchetypes,
    int Products,
    int Resources,
    int Buildings,
    int Skills,
    int Events,
    int Technologies);
