namespace Metropole.Sim;

public static class SystemicBootstrap
{
    public static void UpgradeLegacySchema(GameState state)
    {
        if (state.SchemaVersion < 1)
            throw new InvalidDataException($"Schema legado não suportado: {state.SchemaVersion}.");
        if (state.SchemaVersion > GameState.CurrentSchemaVersion)
            throw new InvalidDataException($"Save usa schema futuro {state.SchemaVersion}.");

        if (state.SchemaVersion == 1)
        {
            state.SchemaVersion = 2;
            state.RulesVersion = "1.6.0";
        }

        Ensure(state);
    }

    public static void Ensure(GameState state)
    {
        state.Systemic ??= new SystemicSimulationState();
        state.Relationships ??= [];
        state.Memories ??= [];
        state.Residences ??= [];

        foreach (var citizen in state.Citizens)
        {
            if (citizen.Health <= 0m && citizen.Alive) citizen.Health = 90m;
            if (citizen.Hygiene <= 0m && citizen.Alive) citizen.Hygiene = 72m;
            if (citizen.SocialNeed <= 0m && citizen.Alive) citizen.SocialNeed = 58m;
            if (citizen.Fun <= 0m && citizen.Alive) citizen.Fun = 55m;
            if (citizen.Comfort <= 0m && citizen.Alive) citizen.Comfort = 62m;
            if (string.IsNullOrWhiteSpace(citizen.CurrentGoal)) citizen.CurrentGoal = "Manter rotina";
        }

        if (state.Player.Hygiene <= 0m) state.Player.Hygiene = 78m;
        if (state.Player.Fun <= 0m) state.Player.Fun = 62m;
        if (state.Player.Comfort <= 0m) state.Player.Comfort = 68m;
        if (state.Player.Security <= 0m) state.Player.Security = 72m;
        if (string.IsNullOrWhiteSpace(state.Player.CurrentGoal))
            state.Player.CurrentGoal = "Manter uma rotina equilibrada";

        if (state.Residences.Count == 0)
            EnsureResidences(state);
        SystemicSimulation.ReclassifyPopulation(state);
        SynchronizePartnerRelations(state);
    }

    private static void EnsureResidences(GameState state)
    {
        var byHousehold = state.Citizens
            .Where(c => c.Alive)
            .GroupBy(c => c.HouseholdId)
            .OrderBy(g => g.Key)
            .ToArray();

        var existing = state.Residences.ToDictionary(r => r.HouseholdId);
        foreach (var household in byHousehold)
        {
            var first = household.First();
            if (!existing.TryGetValue(household.Key, out var residence))
            {
                var district = state.Districts.FirstOrDefault(d => d.Id == first.DistrictId);
                residence = new ResidenceState
                {
                    HouseholdId = household.Key,
                    DistrictId = first.DistrictId,
                    Capacity = Math.Max(2, household.Count() + 1),
                    Quality = Math.Clamp(0.48m + (district?.WealthIndex ?? 1m) * 0.12m, 0.35m, 0.92m),
                    MonthlyRent = decimal.Round(180m * (district?.RentIndex ?? 1m), 2)
                };
                state.Residences.Add(residence);
                existing[household.Key] = residence;
            }

            residence.DistrictId = first.DistrictId;
            residence.ResidentIds = household.Select(c => c.Id).OrderBy(id => id).ToList();
            residence.Capacity = Math.Max(residence.Capacity, residence.ResidentIds.Count);
        }

        var livingHouseholds = byHousehold.Select(g => g.Key).ToHashSet();
        state.Residences.RemoveAll(r => !livingHouseholds.Contains(r.HouseholdId));
    }

    private static void SynchronizePartnerRelations(GameState state)
    {
        foreach (var citizen in state.Citizens.Where(c => c.Alive && c.PartnerCitizenId is not null))
        {
            var partnerId = citizen.PartnerCitizenId!.Value;
            if (citizen.Id >= partnerId) continue;
            var relation = SystemicSimulation.GetOrCreateRelation(state, citizen.Id, partnerId);
            relation.Familiarity = Math.Max(relation.Familiarity, 0.75m);
            relation.Friendship = Math.Max(relation.Friendship, 0.58m);
            relation.Trust = Math.Max(relation.Trust, 0.55m);
            relation.Attraction = Math.Max(relation.Attraction, 0.60m);
            relation.Romance = Math.Max(relation.Romance, 0.68m);
        }
    }
}

public static class SystemicSimulation
{
    public static void ReclassifyPopulation(GameState state)
    {
        var playerDistrict = state.Districts.FirstOrDefault(d => d.Id == state.Player.DistrictId);
        if (playerDistrict is null) return;

        var systemic = state.Systemic;
        var sameDistrict = state.Citizens
            .Where(c => c.Alive && c.DistrictId == state.Player.DistrictId)
            .OrderByDescending(c => InterestScore(c, state.CurrentDay))
            .ThenBy(c => c.Id)
            .ToArray();

        var interactiveIds = sameDistrict
            .Take(Math.Max(1, systemic.MaxInteractiveCitizens))
            .Select(c => c.Id)
            .ToHashSet();

        var activeIds = sameDistrict
            .Skip(interactiveIds.Count)
            .Take(Math.Max(0, systemic.MaxActiveCitizens - interactiveIds.Count))
            .Select(c => c.Id)
            .ToHashSet();

        foreach (var citizen in state.Citizens.Where(c => c.Alive))
        {
            if (interactiveIds.Contains(citizen.Id))
            {
                citizen.SimulationDetail = PopulationDetailLevel.Interactive;
                continue;
            }

            if (activeIds.Contains(citizen.Id))
            {
                citizen.SimulationDetail = PopulationDetailLevel.Active;
                continue;
            }

            var district = state.Districts.FirstOrDefault(d => d.Id == citizen.DistrictId);
            if (district is not null &&
                Math.Abs(district.GridX - playerDistrict.GridX) + Math.Abs(district.GridY - playerDistrict.GridY) <= 1)
            {
                citizen.SimulationDetail = PopulationDetailLevel.Regional;
                continue;
            }

            citizen.SimulationDetail = PopulationDetailLevel.Abstract;
        }

        systemic.LastReclassificationDay = state.CurrentDay;
    }

    public static void UpdateCitizensHour(GameState state)
    {
        if (state.Systemic.LastReclassificationDay != state.CurrentDay)
            ReclassifyPopulation(state);

        var hour = state.CurrentHour;
        foreach (var citizen in state.Citizens.Where(c => c.Alive))
        {
            var cadence = citizen.SimulationDetail switch
            {
                PopulationDetailLevel.Interactive => 1,
                PopulationDetailLevel.Active => 1,
                PopulationDetailLevel.Regional => 2,
                _ => 4
            };

            if ((hour + citizen.Id) % cadence != 0) continue;
            var step = (decimal)cadence;

            var decision = CitizenDecisionSystem.Decide(citizen, hour);
            citizen.CurrentActivity = decision.Activity;
            citizen.CurrentGoal = decision.Goal;
            state.Systemic.TotalCitizenDecisions++;

            citizen.Hunger = Math.Clamp(citizen.Hunger + 0.55m * step, 0m, 100m);
            citizen.Hygiene = Math.Clamp(citizen.Hygiene - 0.18m * step, 0m, 100m);
            citizen.SocialNeed = Math.Clamp(citizen.SocialNeed - 0.13m * step, 0m, 100m);
            citizen.Fun = Math.Clamp(citizen.Fun - 0.10m * step, 0m, 100m);
            citizen.Comfort = Math.Clamp(citizen.Comfort - 0.04m * step, 0m, 100m);

            switch (decision.Activity)
            {
                case "Dormindo":
                    citizen.Energy = Math.Clamp(citizen.Energy + 4.4m * step, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress - 0.7m * step, 0m, 100m);
                    citizen.Comfort = Math.Clamp(citizen.Comfort + 0.45m * step, 0m, 100m);
                    break;
                case "Trabalhando":
                    citizen.Energy = Math.Clamp(citizen.Energy - 2.1m * step, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress + 0.65m * step, 0m, 100m);
                    break;
                case "Estudando":
                    citizen.Energy = Math.Clamp(citizen.Energy - 1.1m * step, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress + 0.25m * step, 0m, 100m);
                    break;
                case "Comendo":
                    citizen.Hunger = Math.Clamp(citizen.Hunger - 8m * step, 0m, 100m);
                    citizen.Happiness = Math.Clamp(citizen.Happiness + 0.20m * step, 0m, 100m);
                    break;
                case "Cuidando da higiene":
                    citizen.Hygiene = Math.Clamp(citizen.Hygiene + 7m * step, 0m, 100m);
                    break;
                case "Socializando":
                    citizen.SocialNeed = Math.Clamp(citizen.SocialNeed + 4m * step, 0m, 100m);
                    citizen.Happiness = Math.Clamp(citizen.Happiness + 0.30m * step, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress - 0.30m * step, 0m, 100m);
                    break;
                case "Lazer":
                    citizen.Fun = Math.Clamp(citizen.Fun + 3.2m * step, 0m, 100m);
                    citizen.Happiness = Math.Clamp(citizen.Happiness + 0.35m * step, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress - 0.35m * step, 0m, 100m);
                    break;
                default:
                    citizen.Energy = Math.Clamp(citizen.Energy + 0.20m * step, 0m, 100m);
                    break;
            }

            if (citizen.Hunger > 88m || citizen.Energy < 12m || citizen.Hygiene < 15m)
                citizen.Health = Math.Clamp(citizen.Health - 0.20m * step, 0m, 100m);
            else
                citizen.Health = Math.Clamp(citizen.Health + 0.03m * step, 0m, 100m);
        }
    }

    public static SocialRelationState GetOrCreateRelation(GameState state, int citizenAId, int citizenBId)
    {
        if (citizenAId == citizenBId) throw new ArgumentException("Uma relação exige duas pessoas.");
        var a = Math.Min(citizenAId, citizenBId);
        var b = Math.Max(citizenAId, citizenBId);

        var relation = state.Relationships.FirstOrDefault(r => r.CitizenAId == a && r.CitizenBId == b);
        if (relation is not null) return relation;

        relation = new SocialRelationState
        {
            CitizenAId = a,
            CitizenBId = b,
            Familiarity = 0.08m,
            Friendship = 0.02m,
            Trust = 0.04m,
            Respect = 0.04m
        };
        state.Relationships.Add(relation);
        return relation;
    }

    public static SystemicSnapshot Snapshot(GameState state)
    {
        var alive = state.Citizens.Where(c => c.Alive).ToArray();
        return new SystemicSnapshot(
            alive.Length,
            alive.Count(c => c.SimulationDetail == PopulationDetailLevel.Interactive),
            alive.Count(c => c.SimulationDetail == PopulationDetailLevel.Active),
            alive.Count(c => c.SimulationDetail == PopulationDetailLevel.Regional),
            alive.Count(c => c.SimulationDetail == PopulationDetailLevel.Abstract),
            state.Relationships.Count,
            state.Memories.Count,
            state.Residences.Count,
            alive.Length == 0 ? 0m : alive.Average(c => c.Hygiene),
            alive.Length == 0 ? 0m : alive.Average(c => c.SocialNeed),
            alive.Length == 0 ? 0m : alive.Average(c => c.Fun));
    }

    internal static void ProcessDailySocial(GameState state, DeterministicRng rng)
    {
        var relationMap = state.Relationships.ToDictionary(
            r => RelationKey(r.CitizenAId, r.CitizenBId),
            r => r);

        var candidates = state.Citizens
            .Where(c => c.Alive && c.SimulationDetail is PopulationDetailLevel.Interactive or PopulationDetailLevel.Active)
            .GroupBy(c => c.DistrictId)
            .OrderBy(g => g.Key)
            .ToArray();

        var interactions = 0;
        foreach (var group in candidates)
        {
            var people = group.OrderBy(c => c.Id).ToArray();
            if (people.Length < 2) continue;

            var offset = rng.NextInt(1, people.Length);
            for (var i = 0; i < people.Length && interactions < 28; i += 2)
            {
                var a = people[i];
                var b = people[(i + offset) % people.Length];
                if (a.Id == b.Id) continue;

                var relation = GetOrCreateRelation(state, a.Id, b.Id, relationMap);
                var compatibility =
                    1m
                    - Math.Abs(a.Sociability - b.Sociability) * 0.25m
                    - Math.Abs(a.Ambition - b.Ambition) * 0.15m
                    - Math.Abs(a.Discipline - b.Discipline) * 0.12m;

                var positive = Math.Clamp(compatibility, 0.20m, 1m);
                relation.Familiarity = Clamp01(relation.Familiarity + 0.018m);
                relation.Friendship = Clamp01(relation.Friendship + 0.012m * positive);
                relation.Trust = Clamp01(relation.Trust + 0.007m * positive);
                relation.Respect = Clamp01(relation.Respect + 0.006m * (a.Discipline + b.Discipline) / 2m);
                relation.Attraction = Clamp01(relation.Attraction + 0.004m * positive);
                relation.Resentment = Clamp01(relation.Resentment * 0.985m);
                relation.LastInteractionDay = state.CurrentDay;

                a.SocialNeed = Math.Clamp(a.SocialNeed + 2.5m, 0m, 100m);
                b.SocialNeed = Math.Clamp(b.SocialNeed + 2.5m, 0m, 100m);
                state.Systemic.TotalSocialInteractions++;
                interactions++;

                if (relation.Friendship > 0.30m && (state.CurrentDay + a.Id + b.Id) % 11 == 0)
                {
                    AddMemory(state, a.Id, "Social", $"Conversou com {b.Name}.", 0.25m, 0.25m);
                    AddMemory(state, b.Id, "Social", $"Conversou com {a.Name}.", 0.25m, 0.25m);
                }
            }

            if (interactions >= 28) break;
        }
    }

    private static SocialRelationState GetOrCreateRelation(
        GameState state,
        int citizenAId,
        int citizenBId,
        Dictionary<long, SocialRelationState> relationMap)
    {
        var a = Math.Min(citizenAId, citizenBId);
        var b = Math.Max(citizenAId, citizenBId);
        var key = RelationKey(a, b);
        if (relationMap.TryGetValue(key, out var existing)) return existing;

        var relation = new SocialRelationState
        {
            CitizenAId = a,
            CitizenBId = b,
            Familiarity = 0.08m,
            Friendship = 0.02m,
            Trust = 0.04m,
            Respect = 0.04m
        };
        state.Relationships.Add(relation);
        relationMap[key] = relation;
        return relation;
    }

    private static long RelationKey(int citizenAId, int citizenBId)
    {
        var a = Math.Min(citizenAId, citizenBId);
        var b = Math.Max(citizenAId, citizenBId);
        return ((long)a << 32) | (uint)b;
    }

    internal static void DecayMemories(GameState state)
    {
        foreach (var memory in state.Memories)
        {
            var age = Math.Max(0, state.CurrentDay - memory.Day);
            if (age <= 7) continue;
            memory.Importance = Math.Clamp(memory.Importance - 0.003m * Math.Min(30, age), 0m, 1m);
            memory.EmotionalWeight *= 0.995m;
        }

        state.Memories.RemoveAll(m => m.Importance < 0.035m || state.CurrentDay - m.Day > 365);
        if (state.Memories.Count > 4_000)
            state.Memories = state.Memories
                .OrderByDescending(m => m.Importance)
                .ThenByDescending(m => m.Day)
                .Take(4_000)
                .ToList();
    }

    internal static void RefreshResidences(GameState state)
    {
        var livingByHousehold = state.Citizens
            .Where(c => c.Alive)
            .GroupBy(c => c.HouseholdId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Id).OrderBy(id => id).ToList());

        foreach (var residence in state.Residences)
        {
            residence.ResidentIds = livingByHousehold.GetValueOrDefault(residence.HouseholdId) ?? [];
            residence.Capacity = Math.Max(residence.Capacity, residence.ResidentIds.Count);
        }
    }

    private static decimal InterestScore(CitizenState citizen, int day)
    {
        var urgency =
            citizen.Hunger
            + (100m - citizen.Energy)
            + (100m - citizen.Hygiene) * 0.5m
            + citizen.Stress * 0.6m;
        return urgency + citizen.Sociability * 20m + ((citizen.Id * 31 + day * 17) % 37);
    }

    private static void AddMemory(
        GameState state,
        int citizenId,
        string kind,
        string summary,
        decimal emotionalWeight,
        decimal importance)
    {
        state.Memories.Add(new CitizenMemoryState
        {
            CitizenId = citizenId,
            Day = state.CurrentDay,
            Kind = kind,
            Summary = summary,
            EmotionalWeight = emotionalWeight,
            Importance = importance
        });
    }

    private static decimal Clamp01(decimal value) => Math.Clamp(value, 0m, 1m);
}

public static class CitizenDecisionSystem
{
    public sealed record Decision(string Goal, string Activity, decimal Score);

    public static Decision Decide(CitizenState citizen, int hour)
    {
        var options = new List<Decision>(8);

        Add(options, "Recuperar energia", "Dormindo",
            (100m - citizen.Energy) * 1.15m + (hour is >= 0 and < 6 ? 42m : 0m));

        Add(options, "Alimentar-se", "Comendo",
            citizen.Hunger * 1.20m);

        Add(options, "Cuidar da higiene", "Cuidando da higiene",
            (100m - citizen.Hygiene) * 0.85m);

        Add(options, "Manter vínculos sociais", "Socializando",
            (100m - citizen.SocialNeed) * (0.55m + citizen.Sociability * 0.35m));

        Add(options, "Descansar e se divertir", "Lazer",
            (100m - citizen.Fun) * 0.58m);

        if (citizen.EmployedCompanyId is not null && hour is >= 8 and < 17)
            Add(options, "Cumprir jornada de trabalho", "Trabalhando",
                88m + citizen.Discipline * 22m - citizen.Hunger * 0.16m - (100m - citizen.Energy) * 0.14m);

        if (citizen.AgeYears is >= 6 and <= 22 && hour is >= 8 and < 15)
            Add(options, "Estudar e desenvolver habilidades", "Estudando",
                82m + citizen.Ambition * 18m);

        Add(options, hour is 7 or 17 ? "Deslocar-se" : "Manter rotina doméstica",
            hour is 7 or 17 ? "Deslocando-se" : "Em casa",
            hour is 7 or 17 ? 46m : 24m + citizen.Comfort * 0.20m);

        return options
            .OrderByDescending(o => o.Score)
            .ThenBy(o => o.Activity, StringComparer.Ordinal)
            .First();
    }

    private static void Add(List<Decision> options, string goal, string activity, decimal score) =>
        options.Add(new Decision(goal, activity, Math.Max(0m, score)));
}

public static class AffordanceCatalog
{
    private static readonly AffordanceDefinition[] Definitions =
    [
        new("sleep", "Dormir 8h", "Cama", 8, "Recupera energia, conforto e reduz estresse"),
        new("eat", "Comer", "Geladeira / alimentação", 1, "Reduz fome usando o mercado real"),
        new("shower", "Tomar banho 1h", "Chuveiro", 1, "Recupera higiene e conforto"),
        new("relax", "Relaxar 2h", "Sofá / TV", 2, "Recupera diversão e reduz estresse"),
        new("socialize", "Socializar 2h", "Celular / espaços sociais", 2, "Recupera vida social e felicidade"),
        new("study", "Estudar 2h", "Computador / estudo", 2, "Avança educação com custo real")
    ];

    public static IReadOnlyList<AffordanceDefinition> All => Definitions;
}

public static class SystemicPlayerActions
{
    public static IReadOnlyList<AffordanceDefinition> GetPlayerAffordances(this SimulationEngine engine)
    {
        var p = engine.State.Player;
        return AffordanceCatalog.All
            .Where(a => a.Id switch
            {
                "eat" => p.Hunger >= 18m,
                "sleep" => p.Energy <= 92m,
                "shower" => p.Hygiene <= 95m,
                "relax" => p.Fun <= 95m || p.Stress >= 20m,
                "socialize" => p.Social <= 95m,
                "study" => p.EducationLevel < 5,
                _ => true
            })
            .ToArray();
    }

    public static bool PerformPlayerAffordance(this SimulationEngine engine, string affordanceId)
    {
        var p = engine.State.Player;
        switch (affordanceId)
        {
            case "sleep":
                engine.Sleep(8);
                p.Comfort = Math.Clamp(p.Comfort + 18m, 0m, 100m);
                p.CurrentGoal = "Recuperar energia";
                return true;

            case "eat":
                var bought = engine.BuyFood(1);
                if (bought <= 0) return false;
                p.CurrentGoal = "Satisfazer a fome";
                return true;

            case "shower":
                engine.AdvanceHours(1);
                p.Hygiene = 100m;
                p.Comfort = Math.Clamp(p.Comfort + 6m, 0m, 100m);
                p.CurrentActivity = "Cuidando da higiene";
                p.CurrentGoal = "Cuidar da higiene";
                return true;

            case "relax":
                engine.AdvanceHours(2);
                p.Fun = Math.Clamp(p.Fun + 28m, 0m, 100m);
                p.Comfort = Math.Clamp(p.Comfort + 12m, 0m, 100m);
                p.Stress = Math.Clamp(p.Stress - 8m, 0m, 100m);
                p.CurrentActivity = "Lazer";
                p.CurrentGoal = "Recuperar diversão";
                return true;

            case "socialize":
                engine.Socialize(2);
                p.Fun = Math.Clamp(p.Fun + 8m, 0m, 100m);
                p.CurrentGoal = "Fortalecer vínculos sociais";
                return true;

            case "study":
                var studied = engine.Study(2);
                if (studied) p.CurrentGoal = "Desenvolver educação";
                return studied;

            default:
                return false;
        }
    }
}

public sealed partial class SimulationEngine
{
    public SystemicSnapshot GetSystemicSnapshot() => SystemicSimulation.Snapshot(State);

    private void ProcessSystemicSystems(DeterministicRng rng)
    {
        SystemicBootstrap.Ensure(State);
        SystemicSimulation.ReclassifyPopulation(State);
        SystemicSimulation.ProcessDailySocial(State, rng);

        if (State.CurrentDay % 7 == 0)
            SystemicSimulation.DecayMemories(State);

        if (State.CurrentDay % 30 == 0)
        {
            SystemicSimulation.RefreshResidences(State);
            if (State.Relationships.Count > 6_000)
            {
                State.Relationships = State.Relationships
                    .OrderByDescending(r => r.Romance * 2m + r.Friendship + r.Trust + r.Familiarity * 0.5m)
                    .Take(6_000)
                    .ToList();
            }
        }
    }
}
