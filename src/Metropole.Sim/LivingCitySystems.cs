namespace Metropole.Sim;

public static class LivingCityCatalog
{
    public static IReadOnlyList<AffordanceDefinition> Affordances { get; } =
    [
        new("Cama", "Dormir", "Energia", 48m, 480, 0m, 0.01m, 100),
        new("Cama", "Cochilar", "Energia", 20m, 90, 0m, 0.01m, 70),
        new("Geladeira", "Comer", "Fome", 42m, 30, 0m, 0.01m, 95),
        new("Geladeira", "Preparar refeição", "Fome", 58m, 55, 8m, 0.02m, 100),
        new("Pia", "Beber água", "Sede", 55m, 5, 0m, 0.01m, 100),
        new("Chuveiro", "Tomar banho", "Higiene", 62m, 20, 0m, 0.01m, 95),
        new("Banheiro", "Usar banheiro", "Banheiro", 75m, 10, 0m, 0.01m, 100),
        new("Sofá", "Relaxar", "Conforto", 35m, 45, 0m, 0.01m, 60),
        new("TV", "Assistir", "Diversão", 28m, 60, 0m, 0.01m, 50),
        new("Computador", "Conversar online", "Social", 24m, 45, 0m, 0.01m, 55),
        new("Celular", "Mandar mensagem", "Social", 15m, 10, 0m, 0.01m, 45),
        new("Clínica", "Cuidar da saúde", "Saúde", 35m, 90, 45m, 0.03m, 90),
        new("Restaurante", "Fazer refeição", "Fome", 52m, 60, 35m, 0.02m, 85),
        new("Praça", "Socializar", "Social", 32m, 90, 0m, 0.02m, 60)
    ];

    public static AffordanceDefinition? BestFor(string need) =>
        Affordances
            .Where(x => x.SatisfiesNeed.Equals(need, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Cost)
            .FirstOrDefault();
}

public static class LivingCitySystems
{
    public static void EnsureInitialized(GameState state)
    {
        state.LivingCity ??= new LivingCityState();

        foreach (var citizen in state.Citizens)
            EnsureCitizen(state, citizen);

        EnsureFamilyLinks(state);
        EnsureProperties(state);
        EnsureVehicles(state);
        RefreshSimulationLevelsAndRegions(state);

        if (!state.LivingCity.Initialized)
        {
            Publish(state, "LivingCity", "Living City Core inicializado: população, famílias, propriedades, LOD e tráfego abstrato.");
            state.LivingCity.Initialized = true;
        }

        state.LivingCity.SimulationVersion = LivingCityState.CurrentSimulationVersion;
    }

    public static void AdvanceDay(GameState state)
    {
        EnsureInitialized(state);
        state.LivingCity.Tick += 24;

        RefreshSimulationLevels(state);

        var scheduled = 0;
        foreach (var citizen in state.Citizens.Where(x => x.Alive).OrderBy(x => x.Id))
        {
            UpdateNeeds(state, citizen);

            var detailed = citizen.SimulationLevel switch
            {
                SimulationDetailLevel.Interactive => true,
                SimulationDetailLevel.Active => true,
                SimulationDetailLevel.Regional => (state.CurrentDay + citizen.Id) % 2 == 0,
                _ => (state.CurrentDay + citizen.Id) % 7 == 0
            };

            if (detailed)
            {
                PlanGoal(citizen);
                DecayMemories(citizen);
                scheduled++;
            }
        }

        UpdateRelationships(state);
        EnsureFamilyLinks(state);
        EnsureProperties(state);
        EnsureVehicles(state);
        UpdateVehicles(state);
        RefreshRegions(state);

        state.LivingCity.LastMetrics.ScheduledUpdates = scheduled;
        state.LivingCity.LastMetrics.VehiclesInTransit =
            state.LivingCity.Vehicles.Count(v => v.Status == "Em rota");

        if (state.CurrentDay > 0 && state.CurrentDay % 30 == 0)
            Publish(state, "Cidade", BuildMonthlySummary(state));

        PruneEventStream(state);
        Validate(state);
    }

    public static IReadOnlyList<CitizenState> GetCitizensNearDistrict(GameState state, int districtId, int radius)
    {
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        var origin = state.Districts.FirstOrDefault(x => x.Id == districtId)
            ?? throw new ArgumentOutOfRangeException(nameof(districtId));

        var eligibleDistrictIds = state.Districts
            .Where(x => Math.Abs(x.GridX - origin.GridX) + Math.Abs(x.GridY - origin.GridY) <= radius)
            .Select(x => x.Id)
            .ToHashSet();

        return state.Citizens
            .Where(x => x.Alive && eligibleDistrictIds.Contains(x.DistrictId))
            .OrderBy(x => x.Id)
            .ToArray();
    }

    public static IReadOnlyList<CitizenState> GetScheduledBatch(GameState state, int budget)
    {
        if (budget <= 0) return Array.Empty<CitizenState>();
        EnsureInitialized(state);

        var ordered = state.Citizens
            .Where(x => x.Alive)
            .OrderByDescending(x => x.SimulationLevel)
            .ThenBy(x => x.Id)
            .ToArray();

        if (ordered.Length == 0) return Array.Empty<CitizenState>();

        var count = Math.Min(budget, ordered.Length);
        var start = Math.Abs(state.LivingCity.SchedulerCursor) % ordered.Length;
        var result = new CitizenState[count];
        for (var i = 0; i < count; i++)
            result[i] = ordered[(start + i) % ordered.Length];

        state.LivingCity.SchedulerCursor = (start + count) % ordered.Length;
        return result;
    }

    public static void FocusCitizen(GameState state, int? citizenId)
    {
        if (citizenId is not null && !state.Citizens.Any(x => x.Id == citizenId && x.Alive))
            throw new ArgumentOutOfRangeException(nameof(citizenId));

        state.LivingCity.FocusCitizenId = citizenId;
        RefreshSimulationLevels(state);
    }

    private static void EnsureCitizen(GameState state, CitizenState citizen)
    {
        citizen.Needs ??= new CitizenNeedsState();
        citizen.Personality ??= new CitizenPersonalityState();
        citizen.Memories ??= [];
        citizen.Relationships ??= new Dictionary<int, CitizenRelationshipState>();
        citizen.ParentCitizenIds ??= [];
        citizen.ChildCitizenIds ??= [];

        if (citizen.LivingCityInitialized) return;

        var rng = new DeterministicRng(CitizenSeed(state.Seed, citizen.Id));
        citizen.HomeDistrictId = citizen.DistrictId;
        citizen.TargetDistrictId = citizen.DistrictId;

        citizen.Needs = new CitizenNeedsState
        {
            Thirst = rng.NextDecimal(5m, 30m),
            Hygiene = rng.NextDecimal(58m, 98m),
            Bathroom = rng.NextDecimal(2m, 28m),
            Social = rng.NextDecimal(35m, 85m),
            Comfort = rng.NextDecimal(45m, 90m),
            Safety = Clamp01(
                (state.Districts.FirstOrDefault(d => d.Id == citizen.DistrictId)?.SafetyIndex ?? 1m) / 1.4m) * 100m,
            Health = rng.NextDecimal(68m, 98m)
        };

        citizen.Personality = new CitizenPersonalityState
        {
            Extroversion = Clamp01((citizen.Sociability + rng.NextDecimal(0m, 1m)) / 2m),
            Responsibility = Clamp01((citizen.Discipline + rng.NextDecimal(0m, 1m)) / 2m),
            Aggressiveness = rng.NextDecimal(0.05m, 0.70m),
            Generosity = rng.NextDecimal(0.10m, 0.95m),
            Romanticism = rng.NextDecimal(0.10m, 0.95m),
            Courage = Clamp01((citizen.RiskTolerance + rng.NextDecimal(0m, 1m)) / 2m),
            Curiosity = rng.NextDecimal(0.10m, 0.98m),
            Patience = rng.NextDecimal(0.10m, 0.98m)
        };

        citizen.CurrentGoal = "Manter rotina";
        citizen.PlannedAction = "Continuar rotina";
        citizen.LivingCityInitialized = true;
    }

    private static void EnsureFamilyLinks(GameState state)
    {
        foreach (var household in state.Citizens.Where(x => x.Alive).GroupBy(x => x.HouseholdId))
        {
            var adults = household.Where(x => x.AgeYears >= 18).OrderByDescending(x => x.AgeYears).ToArray();
            var minors = household.Where(x => x.AgeYears < 18).ToArray();
            if (adults.Length == 0) continue;

            foreach (var child in minors)
            {
                foreach (var parent in adults.Take(2))
                {
                    if (!child.ParentCitizenIds.Contains(parent.Id))
                        child.ParentCitizenIds.Add(parent.Id);
                    if (!parent.ChildCitizenIds.Contains(child.Id))
                        parent.ChildCitizenIds.Add(child.Id);
                }
            }
        }

        foreach (var citizen in state.Citizens.Where(x => x.PartnerCitizenId is not null))
        {
            var partnerId = citizen.PartnerCitizenId!.Value;
            var relation = GetRelationship(citizen, partnerId);
            relation.Familiarity = Math.Max(relation.Familiarity, 0.85m);
            relation.Friendship = Math.Max(relation.Friendship, 0.65m);
            relation.Trust = Math.Max(relation.Trust, 0.60m);
            relation.Attraction = Math.Max(relation.Attraction, 0.72m);
            relation.Romance = Math.Max(relation.Romance, 0.78m);
        }
    }

    private static void EnsureProperties(GameState state)
    {
        var householdsWithProperty = state.LivingCity.Properties
            .Select(x => x.HouseholdId)
            .ToHashSet();

        foreach (var household in state.Citizens.Where(x => x.Alive).GroupBy(x => x.HouseholdId))
        {
            if (householdsWithProperty.Contains(household.Key)) continue;

            var residents = household.OrderBy(x => x.Id).ToArray();
            if (residents.Length == 0) continue;

            var owner = residents
                .Where(x => x.AgeYears >= 18)
                .OrderByDescending(x => x.AgeYears)
                .ThenByDescending(x => x.Cash)
                .FirstOrDefault();

            var districtId = residents
                .GroupBy(x => x.HomeDistrictId > 0 ? x.HomeDistrictId : x.DistrictId)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .First().Key;
            var district = state.Districts.First(x => x.Id == districtId);
            var value = decimal.Round(42_000m * district.RentIndex * (0.75m + district.WealthIndex * 0.35m), 2);

            state.LivingCity.Properties.Add(new PropertyState
            {
                Id = state.LivingCity.NextPropertyId++,
                DistrictId = districtId,
                HouseholdId = household.Key,
                OwnerCitizenId = owner?.Id,
                Kind = residents.Length >= 5 ? "Casa" : "Apartamento",
                Value = value,
                Rent = decimal.Round(value * 0.0035m, 2),
                Condition = 0.78m,
                Rooms = Math.Clamp(1 + (residents.Length + 1) / 2, 2, 6),
                ResidentCitizenIds = residents.Select(x => x.Id).ToList()
            });

            householdsWithProperty.Add(household.Key);
        }
    }

    private static void EnsureVehicles(GameState state)
    {
        var existingOwners = state.LivingCity.Vehicles.Select(x => x.OwnerCitizenId).ToHashSet();

        foreach (var citizen in state.Citizens
                     .Where(x => x.Alive && x.AgeYears >= 18 && x.EmployedCompanyId is not null)
                     .OrderBy(x => x.Id))
        {
            if (existingOwners.Contains(citizen.Id)) continue;

            var ownershipRoll = PositiveModulo(unchecked(state.Seed + citizen.Id * 97L), 100);
            if (ownershipRoll >= 34) continue;

            var target = state.Companies.FirstOrDefault(x => x.Id == citizen.EmployedCompanyId)?.DistrictId
                         ?? citizen.HomeDistrictId;

            state.LivingCity.Vehicles.Add(new VehicleState
            {
                Id = state.LivingCity.NextVehicleId++,
                OwnerCitizenId = citizen.Id,
                Kind = ownershipRoll < 5 ? "Moto" : "Carro",
                CurrentDistrictId = citizen.HomeDistrictId,
                TargetDistrictId = target,
                RouteProgress = 0m,
                Condition = 0.72m + PositiveModulo(citizen.Id * 31L, 25) / 100m,
                Status = "Estacionado"
            });
            existingOwners.Add(citizen.Id);
        }
    }

    private static void UpdateNeeds(GameState state, CitizenState citizen)
    {
        var n = citizen.Needs;
        n.Thirst = Clamp100(n.Thirst + 6.5m);
        n.Hygiene = Clamp100(n.Hygiene - 4.0m);
        n.Bathroom = Clamp100(n.Bathroom + 5.5m);
        n.Social = Clamp100(n.Social - (citizen.Sociability > 0.60m ? 4.2m : 2.2m));
        n.Comfort = Clamp100(n.Comfort - 1.5m);

        var district = state.Districts.FirstOrDefault(x => x.Id == citizen.DistrictId);
        var districtSafety = Clamp100(((district?.SafetyIndex ?? 1m) / 1.4m) * 100m);
        n.Safety = Clamp100(n.Safety * 0.88m + districtSafety * 0.12m);

        var wellbeing =
            citizen.Energy * 0.25m
            + (100m - citizen.Stress) * 0.25m
            + citizen.Happiness * 0.25m
            + (100m - citizen.Hunger) * 0.25m;
        n.Health = Clamp100(n.Health * 0.97m + wellbeing * 0.03m);

        if (citizen.CurrentActivity == "Dormindo")
        {
            n.Comfort = Clamp100(n.Comfort + 12m);
            n.Bathroom = Clamp100(n.Bathroom + 1m);
        }
        else if (citizen.CurrentActivity is "Lazer" or "Socializando")
        {
            n.Social = Clamp100(n.Social + 10m);
            n.Comfort = Clamp100(n.Comfort + 4m);
        }
        else if (citizen.CurrentActivity == "Em casa")
        {
            n.Hygiene = Clamp100(n.Hygiene + 7m);
            n.Bathroom = Clamp100(n.Bathroom - 10m);
            n.Comfort = Clamp100(n.Comfort + 5m);
        }

        if (n.Thirst > 92m)
        {
            citizen.Stress = Clamp100(citizen.Stress + 2m);
            citizen.Happiness = Clamp100(citizen.Happiness - 1m);
        }
        if (n.Hygiene < 18m)
            citizen.Happiness = Clamp100(citizen.Happiness - 0.8m);
        if (n.Safety < 25m)
            citizen.Stress = Clamp100(citizen.Stress + 1.2m);
    }

    private static void PlanGoal(CitizenState citizen)
    {
        var needs = new (string Need, decimal Urgency)[]
        {
            ("Fome", citizen.Hunger),
            ("Sede", citizen.Needs.Thirst),
            ("Energia", 100m - citizen.Energy),
            ("Higiene", 100m - citizen.Needs.Hygiene),
            ("Banheiro", citizen.Needs.Bathroom),
            ("Social", 100m - citizen.Needs.Social),
            ("Conforto", 100m - citizen.Needs.Comfort),
            ("Segurança", 100m - citizen.Needs.Safety),
            ("Saúde", 100m - citizen.Needs.Health)
        };

        var selected = needs
            .OrderByDescending(x => x.Urgency)
            .ThenBy(x => x.Need, StringComparer.Ordinal)
            .First();

        if (selected.Urgency < 45m)
        {
            citizen.CurrentGoal = citizen.EmployedCompanyId is not null ? "Manter estabilidade profissional" : "Manter rotina";
            citizen.PlannedAction = citizen.EmployedCompanyId is not null ? "Cumprir agenda de trabalho" : "Continuar rotina";
            return;
        }

        citizen.CurrentGoal = selected.Need switch
        {
            "Fome" => "Conseguir alimentação",
            "Sede" => "Hidratar-se",
            "Energia" => "Recuperar energia",
            "Higiene" => "Cuidar da higiene",
            "Banheiro" => "Usar banheiro",
            "Social" => "Buscar contato social",
            "Conforto" => "Descansar em local confortável",
            "Segurança" => "Ir para um local seguro",
            "Saúde" => "Cuidar da saúde",
            _ => "Manter rotina"
        };

        var affordance = LivingCityCatalog.BestFor(selected.Need);
        citizen.PlannedAction = affordance is null
            ? $"Resolver necessidade: {selected.Need}"
            : $"{affordance.ObjectType}: {affordance.Action}";
    }

    private static void UpdateRelationships(GameState state)
    {
        if (state.CurrentDay % 3 != 0) return;

        foreach (var group in state.Citizens
                     .Where(x => x.Alive && x.AgeYears >= 14)
                     .GroupBy(x => x.DistrictId))
        {
            var citizens = group.OrderBy(x => x.Id).ToArray();
            if (citizens.Length < 2) continue;

            var offset = PositiveModulo(state.CurrentDay + group.Key * 13L, citizens.Length);
            var interactions = Math.Min(24, citizens.Length / 2);

            for (var i = 0; i < interactions; i++)
            {
                var a = citizens[(offset + i * 2) % citizens.Length];
                var b = citizens[(offset + i * 2 + 1) % citizens.Length];
                if (a.Id == b.Id) continue;

                UpdateRelationshipOneWay(state, a, b);
                UpdateRelationshipOneWay(state, b, a);
            }
        }
    }

    private static void UpdateRelationshipOneWay(GameState state, CitizenState a, CitizenState b)
    {
        var r = GetRelationship(a, b.Id);
        var similarity = 1m - (
            Math.Abs(a.Sociability - b.Sociability) * 0.30m +
            Math.Abs(a.Ambition - b.Ambition) * 0.22m +
            Math.Abs(a.Discipline - b.Discipline) * 0.20m +
            Math.Abs(a.Personality.Curiosity - b.Personality.Curiosity) * 0.14m);
        similarity = Clamp01(similarity);

        r.Familiarity = Clamp01(r.Familiarity + 0.018m);
        r.Friendship = Clamp01(r.Friendship + (similarity - 0.45m) * 0.020m);
        r.Trust = Clamp01(r.Trust + (b.Personality.Responsibility + b.Personality.Generosity - 1m) * 0.012m);
        r.Respect = Clamp01(r.Respect + (b.Discipline + b.SkillTier / 5m - 1m) * 0.009m);
        r.Resentment = Clamp01(r.Resentment + (b.Personality.Aggressiveness - a.Personality.Patience) * 0.010m);

        if (a.AgeYears >= 18 && b.AgeYears >= 18 && Math.Abs(a.AgeYears - b.AgeYears) <= 15)
        {
            var attractionDelta =
                (a.Personality.Romanticism * 0.45m + similarity * 0.55m - 0.48m) * 0.012m;
            r.Attraction = Clamp01(r.Attraction + attractionDelta);
            r.Romance = Clamp01(r.Romance + Math.Max(0m, r.Attraction + r.Trust - 1.15m) * 0.008m);
        }

        r.LastInteractionDay = state.CurrentDay;

        if (r.Friendship > 0.72m && !a.Memories.Any(x =>
                x.EventType == "Amizade" && x.Participants.Contains(b.Id) && state.CurrentDay - x.Day < 30))
        {
            AddMemory(a, "Amizade", b.Id, state.CurrentDay, 0.55m, 0.62m,
                $"{a.Name} fortaleceu amizade com {b.Name}.");
        }
    }

    private static CitizenRelationshipState GetRelationship(CitizenState citizen, int otherId)
    {
        if (citizen.Relationships.TryGetValue(otherId, out var existing))
            return existing;

        var created = new CitizenRelationshipState();
        citizen.Relationships[otherId] = created;
        return created;
    }

    private static void AddMemory(
        CitizenState citizen,
        string kind,
        int participantId,
        int day,
        decimal emotion,
        decimal importance,
        string summary)
    {
        citizen.Memories.Add(new CitizenMemoryState
        {
            EventType = kind,
            Participants = [participantId],
            Day = day,
            EmotionalWeight = Math.Clamp(emotion, -1m, 1m),
            Importance = Clamp01(importance),
            Decay = 0.985m,
            Summary = summary
        });

        if (citizen.Memories.Count > 48)
            citizen.Memories = citizen.Memories
                .OrderByDescending(x => x.Importance)
                .ThenByDescending(x => x.Day)
                .Take(48)
                .OrderBy(x => x.Day)
                .ToList();
    }

    private static void DecayMemories(CitizenState citizen)
    {
        foreach (var memory in citizen.Memories)
            memory.Importance = Clamp01(memory.Importance * Math.Clamp(memory.Decay, 0m, 1m));

        citizen.Memories.RemoveAll(x => x.Importance < 0.035m);
    }

    private static void UpdateVehicles(GameState state)
    {
        var citizens = state.Citizens.ToDictionary(x => x.Id);
        var companies = state.Companies.ToDictionary(x => x.Id);

        foreach (var vehicle in state.LivingCity.Vehicles)
        {
            if (!citizens.TryGetValue(vehicle.OwnerCitizenId, out var owner) || !owner.Alive)
            {
                vehicle.Status = "Fora de uso";
                continue;
            }

            var targetDistrict = owner.HomeDistrictId > 0 ? owner.HomeDistrictId : owner.DistrictId;
            if (owner.EmployedCompanyId is int companyId &&
                companies.TryGetValue(companyId, out var company) &&
                company.Open)
            {
                targetDistrict = company.DistrictId;
            }

            vehicle.TargetDistrictId = targetDistrict;
            var commuting = vehicle.CurrentDistrictId != vehicle.TargetDistrictId;
            if (!commuting)
            {
                vehicle.Status = "Estacionado";
                vehicle.RouteProgress = 0m;
                continue;
            }

            vehicle.Status = "Em rota";
            vehicle.RouteProgress = Clamp01(vehicle.RouteProgress + 0.55m);
            if (vehicle.RouteProgress >= 1m)
            {
                vehicle.CurrentDistrictId = vehicle.TargetDistrictId;
                vehicle.RouteProgress = 0m;
                vehicle.Status = "Estacionado";
            }

            vehicle.Condition = Math.Max(0.25m, vehicle.Condition - 0.0005m);
        }
    }

    private static void RefreshSimulationLevelsAndRegions(GameState state)
    {
        RefreshSimulationLevels(state);
        RefreshRegions(state);
    }

    private static void RefreshSimulationLevels(GameState state)
    {
        var playerDistrict = state.Districts.FirstOrDefault(x => x.Id == state.Player.DistrictId);
        if (playerDistrict is null) return;

        var metrics = new PopulationSimulationMetrics();

        foreach (var citizen in state.Citizens.Where(x => x.Alive))
        {
            if (state.LivingCity.FocusCitizenId == citizen.Id)
            {
                citizen.SimulationLevel = SimulationDetailLevel.Interactive;
                metrics.InteractiveAgents++;
                continue;
            }

            var district = state.Districts.FirstOrDefault(x => x.Id == citizen.DistrictId);
            var distance = district is null
                ? int.MaxValue
                : Math.Abs(district.GridX - playerDistrict.GridX) + Math.Abs(district.GridY - playerDistrict.GridY);

            citizen.SimulationLevel = distance switch
            {
                0 => SimulationDetailLevel.Active,
                <= 1 => SimulationDetailLevel.Regional,
                _ => SimulationDetailLevel.Abstract
            };

            switch (citizen.SimulationLevel)
            {
                case SimulationDetailLevel.Active:
                    metrics.ActiveAgents++;
                    break;
                case SimulationDetailLevel.Regional:
                    metrics.RegionalAgents++;
                    break;
                default:
                    metrics.AbstractAgents++;
                    break;
            }
        }

        state.LivingCity.LastMetrics = metrics;
    }

    private static void RefreshRegions(GameState state)
    {
        var regions = new List<WorldRegionState>(state.Districts.Count);
        foreach (var district in state.Districts.OrderBy(x => x.Id))
        {
            var population = state.Citizens.Where(x => x.Alive && x.DistrictId == district.Id).ToArray();
            var companies = state.Companies.Where(x => x.Open && x.DistrictId == district.Id).ToArray();
            var vehicles = state.LivingCity.Vehicles.Count(x => x.CurrentDistrictId == district.Id);
            var commuters = population.Count(x => x.EmployedCompanyId is not null);

            regions.Add(new WorldRegionState
            {
                DistrictId = district.Id,
                Population = population.Length,
                ActiveAgents = population.Count(x => x.SimulationLevel is SimulationDetailLevel.Active or SimulationDetailLevel.Interactive),
                RegionalAgents = population.Count(x => x.SimulationLevel == SimulationDetailLevel.Regional),
                AbstractAgents = population.Count(x => x.SimulationLevel == SimulationDetailLevel.Abstract),
                Vehicles = vehicles,
                TrafficLoad = Clamp01((vehicles + commuters * 0.25m) / Math.Max(20m, population.Length * 0.65m)),
                EconomicActivity = Clamp01(companies.Sum(x => Math.Max(0m, x.LastRevenue)) / 180_000m)
            });
        }

        state.LivingCity.Regions = regions;
    }

    private static void Publish(GameState state, string kind, string summary, int? entityId = null)
    {
        state.LivingCity.Events.Add(new LivingCityEventRecord
        {
            Tick = state.LivingCity.Tick,
            Day = state.CurrentDay,
            Kind = kind,
            Summary = summary,
            EntityId = entityId
        });
    }

    private static string BuildMonthlySummary(GameState state)
    {
        var metrics = state.LivingCity.LastMetrics;
        return $"Mês {state.CurrentDay / 30}: {state.Population:N0} habitantes, {state.OpenCompanies:N0} empresas, " +
               $"{metrics.ActiveAgents:N0} agentes ativos, {metrics.RegionalAgents:N0} regionais, " +
               $"{metrics.AbstractAgents:N0} abstratos e {state.LivingCity.Vehicles.Count:N0} veículos persistentes.";
    }

    private static void PruneEventStream(GameState state)
    {
        const int max = 600;
        if (state.LivingCity.Events.Count <= max) return;
        state.LivingCity.Events = state.LivingCity.Events.TakeLast(max).ToList();
    }

    private static void Validate(GameState state)
    {
        if (state.LivingCity.Properties.Any(x => x.Value < 0m || x.Rent < 0m || x.Condition is < 0m or > 1m))
            throw new InvalidDataException("Living City contém imóvel inválido.");
        if (state.LivingCity.Vehicles.Any(x => x.Condition is < 0m or > 1m || x.RouteProgress is < 0m or > 1m))
            throw new InvalidDataException("Living City contém veículo inválido.");
        if (state.Citizens.Any(x =>
                x.Needs.Thirst is < 0m or > 100m ||
                x.Needs.Hygiene is < 0m or > 100m ||
                x.Needs.Bathroom is < 0m or > 100m ||
                x.Needs.Social is < 0m or > 100m ||
                x.Needs.Comfort is < 0m or > 100m ||
                x.Needs.Safety is < 0m or > 100m ||
                x.Needs.Health is < 0m or > 100m))
            throw new InvalidDataException("Living City contém necessidade fora do intervalo.");

        var citizenIds = state.Citizens.Select(x => x.Id).ToHashSet();
        if (state.LivingCity.Vehicles.Any(x => !citizenIds.Contains(x.OwnerCitizenId)))
            throw new InvalidDataException("Veículo aponta para cidadão inexistente.");
    }

    private static long CitizenSeed(long seed, int citizenId) =>
        unchecked(seed ^ ((long)citizenId * -7046029254386353131L));

    private static int PositiveModulo(long value, int modulo)
    {
        var result = value % modulo;
        return (int)(result < 0 ? result + modulo : result);
    }

    private static decimal Clamp01(decimal value) => Math.Clamp(value, 0m, 1m);
    private static decimal Clamp100(decimal value) => Math.Clamp(value, 0m, 100m);
}
