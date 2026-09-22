namespace Metropole.Sim;

public sealed partial class SimulationEngine
{
    public AaaSimulationSnapshot GetAaaSnapshot()
    {
        EnsureAaaWorldInitialized();
        UpdateSimulationLod();
        UpdateRegionAggregates();

        var world = State.Aaa;
        var tiers = world.Citizens.Values.GroupBy(x => x.Lod).ToDictionary(g => g.Key, g => g.Count());
        var avgCongestion = world.TrafficLinks.Count == 0 ? 0m : world.TrafficLinks.Average(x => x.Congestion);

        return new(
            State.Population,
            tiers.GetValueOrDefault(SimulationLodTier.Statistical),
            tiers.GetValueOrDefault(SimulationLodTier.Regional),
            tiers.GetValueOrDefault(SimulationLodTier.Active),
            tiers.GetValueOrDefault(SimulationLodTier.Interactive),
            world.Regions.Count,
            world.Households.Count,
            world.Properties.Count,
            world.Vehicles.Count,
            world.TrafficLinks.Count,
            decimal.Round(avgCongestion, 3),
            world.Scheduler.TotalHourlyTicks,
            world.Scheduler.BatchSize);
    }

    public CitizenSimulationProfile? GetCitizenSimulationProfile(int citizenId)
    {
        EnsureAaaWorldInitialized();
        return State.Aaa.Citizens.GetValueOrDefault(citizenId);
    }

    public IReadOnlyList<InteractionAffordanceDefinition> GetAffordancesForNeed(string need) =>
        AffordanceCatalog.ForNeed(need);

    public void SetInteractiveCitizen(int? citizenId)
    {
        EnsureAaaWorldInitialized();
        if (citizenId is not null && !State.Citizens.Any(c => c.Alive && c.Id == citizenId.Value))
            citizenId = null;
        State.Aaa.InteractiveCitizenId = citizenId;
        UpdateSimulationLod();
    }

    internal void AdvanceAaaHourlyTick()
    {
        EnsureAaaWorldInitialized();
        var world = State.Aaa;
        var alive = State.Citizens.Where(c => c.Alive).OrderBy(c => c.Id).ToArray();
        if (alive.Length == 0)
        {
            world.Scheduler.LastWorkUnits = 0;
            world.Scheduler.TotalHourlyTicks++;
            return;
        }

        var batchSize = Math.Clamp(world.Scheduler.BatchSize, 32, Math.Max(32, alive.Length));
        var work = Math.Min(batchSize, alive.Length);
        var cursor = world.Scheduler.Cursor % alive.Length;

        for (var i = 0; i < work; i++)
        {
            var citizen = alive[(cursor + i) % alive.Length];
            if (!world.Citizens.TryGetValue(citizen.Id, out var profile)) continue;
            UpdateHourlyPressures(citizen, profile);
            ResolveGoal(citizen, profile);
        }

        world.Scheduler.Cursor = (cursor + work) % alive.Length;
        world.Scheduler.LastWorkUnits = work;
        world.Scheduler.TotalHourlyTicks++;
    }

    private void EnsureAaaWorldInitialized()
    {
        var world = State.Aaa;
        RepairAaaCollections(world);

        if (!world.Initialized)
        {
            world.Version = AaaWorldState.CurrentVersion;
            BootstrapRegions(world);
            BootstrapCitizens(world);
            BootstrapHouseholdsAndProperties(world);
            BootstrapVehicles(world);
            BootstrapRoadGraph(world);
            world.Initialized = true;
            world.LastProcessedDay = State.CurrentDay;
            UpdateRegionAggregates();
        }
        else
        {
            BootstrapRegions(world);
            BootstrapRoadGraph(world);

            var aliveCount = State.Citizens.Count(citizen => citizen.Alive);
            if (world.Citizens.Count < aliveCount)
            {
                BootstrapCitizens(world);
                BootstrapHouseholdsAndProperties(world);
                BootstrapVehicles(world);
            }
        }

        world.Scheduler.BatchSize = Math.Clamp(Math.Max(64, State.Population / 10), 64, 256);
        UpdateSimulationLod();
        AaaSimulationValidator.Validate(State);
    }

    private static void RepairAaaCollections(AaaWorldState world)
    {
        world.Citizens ??= new();
        world.Households ??= new();
        world.Properties ??= new();
        world.Vehicles ??= new();
        world.Regions ??= new();
        world.TrafficLinks ??= new();
        world.Scheduler ??= new();
        world.Performance ??= new();
    }

    private void BootstrapRegions(AaaWorldState world)
    {
        foreach (var district in State.Districts.OrderBy(x => x.Id))
        {
            if (world.Regions.ContainsKey(district.Id)) continue;
            world.Regions[district.Id] = new RegionSimulationState { DistrictId = district.Id, Name = district.Name };
        }
    }

    private void BootstrapCitizens(AaaWorldState world)
    {
        foreach (var citizen in State.Citizens.Where(c => c.Alive).OrderBy(c => c.Id))
        {
            if (world.Citizens.ContainsKey(citizen.Id)) continue;

            var rng = new DeterministicRng(unchecked(State.Seed ^ (citizen.Id * 1_000_003L)));
            var district = State.Districts.First(d => d.Id == citizen.DistrictId);
            var profile = new CitizenSimulationProfile
            {
                CitizenId = citizen.Id,
                RegionId = citizen.DistrictId,
                LastEmployerCompanyId = citizen.EmployedCompanyId,
                LastDistrictId = citizen.DistrictId,
                LastPartnerCitizenId = citizen.PartnerCitizenId,
                LastActivity = citizen.CurrentActivity,
                Personality = new PersonalityState
                {
                    Extroversion = citizen.Sociability,
                    Responsibility = citizen.Discipline,
                    Ambition = citizen.Ambition,
                    Sociability = citizen.Sociability,
                    Aggressiveness = rng.NextDecimal(0.08m, 0.88m),
                    Generosity = rng.NextDecimal(0.15m, 0.92m),
                    Romanticism = rng.NextDecimal(0.12m, 0.96m),
                    Courage = rng.NextDecimal(0.10m, 0.94m),
                    Curiosity = rng.NextDecimal(0.18m, 0.98m),
                    Patience = rng.NextDecimal(0.12m, 0.95m)
                },
                Needs = new NeedPressureState
                {
                    Hunger = citizen.Hunger,
                    Thirst = rng.NextDecimal(8m, 32m),
                    Fatigue = 100m - citizen.Energy,
                    Hygiene = rng.NextDecimal(8m, 38m),
                    Bathroom = rng.NextDecimal(4m, 28m),
                    Social = Clamp(65m - citizen.Happiness * 0.45m - citizen.Sociability * 15m, 0m, 100m),
                    Fun = Clamp(62m - citizen.Happiness * 0.50m, 0m, 100m),
                    Comfort = rng.NextDecimal(5m, 24m),
                    Safety = Clamp((1m - district.SafetyIndex / 1.40m) * 55m, 0m, 60m),
                    Health = Clamp(citizen.Stress * 0.35m, 0m, 100m)
                },
                Routine = BuildRoutine(citizen)
            };

            ResolveGoal(citizen, profile);
            world.Citizens[citizen.Id] = profile;
        }
    }

    private List<RoutineSlot> BuildRoutine(CitizenState citizen)
    {
        var workDistrict = citizen.EmployedCompanyId is int companyId
            ? State.Companies.FirstOrDefault(c => c.Id == companyId)?.DistrictId
            : null;

        var routine = new List<RoutineSlot>
        {
            new() { StartHour = 0, EndHour = 6, Activity = "Dormir", TargetDistrictId = citizen.DistrictId },
            new() { StartHour = 6, EndHour = 8, Activity = "Casa", TargetDistrictId = citizen.DistrictId }
        };

        if (citizen.AgeYears is >= 6 and <= 22)
            routine.Add(new RoutineSlot { StartHour = 8, EndHour = 15, Activity = "Estudar", TargetDistrictId = citizen.DistrictId });
        else if (citizen.EmployedCompanyId is not null)
            routine.Add(new RoutineSlot { StartHour = 8, EndHour = 17, Activity = "Trabalhar", TargetDistrictId = workDistrict });

        routine.Add(new RoutineSlot { StartHour = 18, EndHour = 22, Activity = "Vida pessoal", TargetDistrictId = citizen.DistrictId });
        routine.Add(new RoutineSlot { StartHour = 22, EndHour = 24, Activity = "Casa", TargetDistrictId = citizen.DistrictId });
        return routine;
    }

    private void BootstrapHouseholdsAndProperties(AaaWorldState world)
    {
        foreach (var group in State.Citizens.Where(c => c.Alive)
                     .GroupBy(c => c.HouseholdId > 0 ? c.HouseholdId : 1_000_000 + c.Id).OrderBy(g => g.Key))
        {
            var residents = group.OrderBy(c => c.Id).ToArray();
            if (!world.Households.TryGetValue(group.Key, out var household))
            {
                var propertyId = world.NextPropertyId++;
                household = new HouseholdState
                {
                    Id = group.Key,
                    DistrictId = residents[0].DistrictId,
                    PropertyId = propertyId
                };
                world.Households[group.Key] = household;

                var district = State.Districts.First(d => d.Id == residents[0].DistrictId);
                var owner = residents.Where(c => c.AgeYears >= 18).OrderByDescending(c => c.Cash).FirstOrDefault();
                world.Properties[propertyId] = new PropertyState
                {
                    Id = propertyId,
                    DistrictId = residents[0].DistrictId,
                    Kind = residents.Length >= 4 ? "Casa" : "Apartamento",
                    OwnerCitizenId = owner?.Id,
                    EstimatedValue = decimal.Round((75_000m + residents.Length * 18_000m) * district.WealthIndex, 2),
                    MonthlyRent = decimal.Round((850m + residents.Length * 160m) * district.RentIndex, 2),
                    Condition = 0.82m,
                    Rooms = Math.Clamp(2 + residents.Length / 2, 2, 7)
                };
            }

            household.DistrictId = residents[0].DistrictId;
            household.ResidentCitizenIds = residents.Select(c => c.Id).ToList();
            if (world.Properties.TryGetValue(household.PropertyId, out var property))
            {
                property.DistrictId = household.DistrictId;
                property.ResidentCitizenIds = household.ResidentCitizenIds.ToList();
            }
        }
    }

    private void BootstrapVehicles(AaaWorldState world)
    {
        var owners = world.Vehicles.Values.Select(v => v.OwnerCitizenId).ToHashSet();
        foreach (var citizen in State.Citizens
                     .Where(c => c.Alive && c.AgeYears >= 18 && c.Cash >= 4_000m && c.Id % 4 == 0)
                     .OrderBy(c => c.Id))
        {
            if (owners.Contains(citizen.Id)) continue;
            var id = world.NextVehicleId++;
            world.Vehicles[id] = new VehicleState
            {
                Id = id,
                OwnerCitizenId = citizen.Id,
                Type = citizen.Cash > 12_000m ? "Carro" : "Moto",
                CurrentDistrictId = citizen.DistrictId,
                Condition = 0.82m + (citizen.Id % 12) / 100m,
                EstimatedValue = citizen.Cash > 12_000m ? 28_000m : 9_000m
            };
        }
    }

    private void BootstrapRoadGraph(AaaWorldState world)
    {
        foreach (var a in State.Districts.OrderBy(d => d.Id))
        foreach (var b in State.Districts.Where(d => d.Id > a.Id))
        {
            if (Math.Abs(a.GridX - b.GridX) + Math.Abs(a.GridY - b.GridY) != 1) continue;
            if (world.TrafficLinks.Any(x =>
                    (x.FromDistrictId == a.Id && x.ToDistrictId == b.Id) ||
                    (x.FromDistrictId == b.Id && x.ToDistrictId == a.Id)))
                continue;
            world.TrafficLinks.Add(new TrafficLinkState
            {
                FromDistrictId = a.Id,
                ToDistrictId = b.Id,
                Capacity = 160 + (a.Id + b.Id) % 5 * 35
            });
        }
    }

    private void ProcessAaaSystems(DeterministicRng rng)
    {
        EnsureAaaWorldInitialized();
        var world = State.Aaa;
        if (world.LastProcessedDay == State.CurrentDay) return;

        world.LastProcessedDay = State.CurrentDay;

        var aliveCount = State.Citizens.Count(citizen => citizen.Alive);
        if (world.Citizens.Count < aliveCount)
        {
            BootstrapCitizens(world);
            BootstrapHouseholdsAndProperties(world);
            BootstrapVehicles(world);
        }

        foreach (var citizen in State.Citizens.Where(c => c.Alive).OrderBy(c => c.Id))
        {
            if (!world.Citizens.TryGetValue(citizen.Id, out var profile)) continue;
            profile.RegionId = citizen.DistrictId;
            UpdateDailyPressures(citizen, profile, rng);
            DetectImportantChanges(citizen, profile);
            DecayMemories(profile);
            ResolveGoal(citizen, profile);
        }

        UpdateSimulationLod();

        // Expensive aggregate reconciliation runs at controlled cadences.
        // It does not affect economic truth, only derived world telemetry.
        if (State.CurrentDay % 7 == 0 || State.CurrentDay <= 1)
        {
            UpdateHouseholdLocations();
            UpdateAbstractTraffic();
            UpdateRegionAggregates();
        }

        AaaSimulationValidator.Validate(State);
    }

    private static void UpdateHourlyPressures(CitizenState citizen, CitizenSimulationProfile profile)
    {
        var needs = profile.Needs;
        needs.Hunger = citizen.Hunger;
        needs.Fatigue = 100m - citizen.Energy;
        needs.Thirst = Clamp(needs.Thirst + 1.8m, 0m, 100m);
        needs.Hygiene = Clamp(needs.Hygiene + 0.55m, 0m, 100m);
        needs.Bathroom = Clamp(needs.Bathroom + 1.2m, 0m, 100m);
        needs.Fun = Clamp(needs.Fun + (citizen.CurrentActivity == "Lazer" ? -3.5m : 0.65m), 0m, 100m);
        needs.Social = Clamp(needs.Social + (citizen.CurrentActivity == "Lazer" ? -2.8m : 0.35m), 0m, 100m);
        needs.Comfort = Clamp(needs.Comfort + (citizen.CurrentActivity == "Em casa" ? -1.2m : 0.25m), 0m, 100m);
        if (citizen.CurrentActivity == "Dormindo") needs.Fatigue = Clamp(needs.Fatigue - 12m, 0m, 100m);
        if (citizen.CurrentActivity == "Em casa" && needs.Hygiene > 70m) needs.Hygiene = Clamp(needs.Hygiene - 24m, 0m, 100m);
        if (citizen.CurrentActivity == "Em casa" && needs.Bathroom > 65m) needs.Bathroom = Clamp(needs.Bathroom - 45m, 0m, 100m);
    }

    private void UpdateDailyPressures(CitizenState citizen, CitizenSimulationProfile profile, DeterministicRng rng)
    {
        var district = State.Districts.First(d => d.Id == citizen.DistrictId);
        var needs = profile.Needs;
        needs.Hunger = citizen.Hunger;
        needs.Fatigue = 100m - citizen.Energy;
        needs.Thirst = Clamp(needs.Thirst + rng.NextDecimal(-8m, 10m), 4m, 88m);
        needs.Hygiene = Clamp(needs.Hygiene + rng.NextDecimal(-22m, 12m), 3m, 92m);
        needs.Bathroom = Clamp(needs.Bathroom + rng.NextDecimal(-38m, 16m), 2m, 94m);
        needs.Social = Clamp(62m - citizen.Happiness * 0.42m - citizen.Sociability * 14m + citizen.Stress * 0.15m, 0m, 100m);
        needs.Fun = Clamp(66m - citizen.Happiness * 0.54m + citizen.Stress * 0.18m, 0m, 100m);
        needs.Comfort = Clamp(needs.Comfort + rng.NextDecimal(-10m, 8m), 2m, 86m);
        needs.Safety = Clamp((1m - district.SafetyIndex / 1.40m) * 58m + citizen.Stress * 0.08m, 0m, 100m);
        needs.Health = Clamp(citizen.Stress * 0.32m + (100m - citizen.Energy) * 0.18m, 0m, 100m);
    }

    private void DetectImportantChanges(CitizenState citizen, CitizenSimulationProfile profile)
    {
        if (profile.LastEmployerCompanyId != citizen.EmployedCompanyId)
        {
            var text = citizen.EmployedCompanyId is int companyId ? $"Começou a trabalhar na empresa {companyId}." : "Ficou sem emprego.";
            AddCitizenMemory(profile, "Carreira", text, 0.55m, 0.78m, citizen.EmployedCompanyId);
            profile.LastEmployerCompanyId = citizen.EmployedCompanyId;
        }
        if (profile.LastDistrictId != citizen.DistrictId)
        {
            AddCitizenMemory(profile, "Mudança", $"Mudou para o distrito {citizen.DistrictId}.", 0.42m, 0.65m, citizen.DistrictId);
            profile.LastDistrictId = citizen.DistrictId;
        }
        if (profile.LastPartnerCitizenId != citizen.PartnerCitizenId)
        {
            var summary = citizen.PartnerCitizenId is int partnerId ? $"Criou vínculo romântico com o cidadão {partnerId}." : "Um relacionamento terminou.";
            AddCitizenMemory(profile, "Relacionamento", summary, 0.72m, 0.90m, citizen.PartnerCitizenId);
            profile.LastPartnerCitizenId = citizen.PartnerCitizenId;
        }
        profile.LastActivity = citizen.CurrentActivity;
    }

    private void AddCitizenMemory(CitizenSimulationProfile profile, string eventType, string summary,
        decimal emotionalWeight, decimal importance, int? relatedEntityId)
    {
        var world = State.Aaa;
        profile.Memories.Add(new CitizenMemoryState
        {
            Id = world.NextMemoryId++,
            Day = State.CurrentDay,
            EventType = eventType,
            Summary = summary,
            EmotionalWeight = Clamp(emotionalWeight, -1m, 1m),
            Importance = Clamp(importance, 0m, 1m),
            RelatedEntityId = relatedEntityId
        });
        if (profile.Memories.Count > 64) profile.Memories.RemoveRange(0, profile.Memories.Count - 64);
    }

    private void DecayMemories(CitizenSimulationProfile profile)
    {
        foreach (var memory in profile.Memories)
        {
            memory.Importance = Clamp(memory.Importance * 0.997m, 0m, 1m);
            memory.EmotionalWeight = Clamp(memory.EmotionalWeight * 0.998m, -1m, 1m);
        }
        profile.Memories.RemoveAll(m => State.CurrentDay - m.Day > 365 && m.Importance < 0.16m);
    }

    private void ResolveGoal(CitizenState citizen, CitizenSimulationProfile profile)
    {
        var needs = profile.Needs;
        var candidates = new (string Need, decimal Pressure, string Action)[]
        {
            ("Fome", needs.Hunger, "Buscar alimentação"),
            ("Sede", needs.Thirst, "Beber"),
            ("Fadiga", needs.Fatigue, "Dormir"),
            ("Higiene", needs.Hygiene, "Tomar banho"),
            ("Banheiro", needs.Bathroom, "Usar banheiro"),
            ("Social", needs.Social * (0.65m + profile.Personality.Extroversion * 0.35m), "Socializar"),
            ("Diversão", needs.Fun * (0.70m + profile.Personality.Curiosity * 0.30m), "Buscar lazer"),
            ("Conforto", needs.Comfort, "Descansar"),
            ("Segurança", needs.Safety * (1.15m - profile.Personality.Courage * 0.25m), "Buscar lugar seguro"),
            ("Saúde", needs.Health, "Cuidar da saúde")
        };
        var best = candidates.OrderByDescending(x => x.Pressure).ThenBy(x => x.Need, StringComparer.Ordinal).First();
        var affordance = AffordanceCatalog.BestForNeed(best.Need, citizen.Cash);
        var createdDay = profile.CurrentGoal.Need == best.Need ? profile.CurrentGoal.CreatedDay : State.CurrentDay;
        profile.CurrentGoal = new CitizenGoalState
        {
            Need = best.Need,
            Action = best.Action,
            PreferredAffordanceId = affordance?.Id,
            Utility = decimal.Round(Clamp(best.Pressure / 100m, 0m, 1m), 3),
            CreatedDay = createdDay
        };
    }

    private void UpdateSimulationLod()
    {
        var world = State.Aaa;
        var playerDistrict = State.Player.DistrictId;
        var playerRegion = State.Districts.FirstOrDefault(d => d.Id == playerDistrict);
        if (playerRegion is null) return;
        var activeCap = Math.Max(1, world.Performance.ActiveCitizenCap);
        var activeAssigned = 0;

        foreach (var citizen in State.Citizens.Where(c => c.Alive).OrderBy(c => c.Id))
        {
            if (!world.Citizens.TryGetValue(citizen.Id, out var profile)) continue;
            if (world.InteractiveCitizenId == citizen.Id)
            {
                profile.Lod = SimulationLodTier.Interactive;
                continue;
            }
            if (citizen.DistrictId == playerDistrict && activeAssigned < activeCap)
            {
                profile.Lod = SimulationLodTier.Active;
                activeAssigned++;
                continue;
            }
            var district = State.Districts.FirstOrDefault(d => d.Id == citizen.DistrictId);
            if (district is not null &&
                Math.Abs(district.GridX - playerRegion.GridX) + Math.Abs(district.GridY - playerRegion.GridY) <= 1)
            {
                profile.Lod = SimulationLodTier.Regional;
                continue;
            }
            profile.Lod = SimulationLodTier.Statistical;
        }
    }

    private void UpdateHouseholdLocations()
    {
        var world = State.Aaa;
        var citizens = State.Citizens.Where(c => c.Alive).ToDictionary(c => c.Id);
        foreach (var household in world.Households.Values)
        {
            var resident = household.ResidentCitizenIds.Select(id => citizens.GetValueOrDefault(id)).FirstOrDefault(c => c is not null);
            if (resident is not null) household.DistrictId = resident.DistrictId;
            if (world.Properties.TryGetValue(household.PropertyId, out var property))
            {
                property.DistrictId = household.DistrictId;
                property.ResidentCitizenIds = household.ResidentCitizenIds.Where(citizens.ContainsKey).ToList();
            }
        }
    }

    private void UpdateAbstractTraffic()
    {
        var world = State.Aaa;
        foreach (var link in world.TrafficLinks)
        {
            link.DailyDemand = 0;
            link.Congestion = 0m;
        }

        var districtsById = State.Districts.ToDictionary(d => d.Id);
        var districtsByCoord = State.Districts.ToDictionary(d => (d.GridX, d.GridY));
        var companies = State.Companies.Where(c => c.Open).ToDictionary(c => c.Id);

        foreach (var citizen in State.Citizens.Where(c => c.Alive && c.EmployedCompanyId is not null))
        {
            if (!companies.TryGetValue(citizen.EmployedCompanyId!.Value, out var company)) continue;
            if (company.DistrictId == citizen.DistrictId) continue;
            if (!districtsById.TryGetValue(citizen.DistrictId, out var from)) continue;
            if (!districtsById.TryGetValue(company.DistrictId, out var to)) continue;

            var x = from.GridX;
            var y = from.GridY;
            while (x != to.GridX)
            {
                var nextX = x + Math.Sign(to.GridX - x);
                if (!districtsByCoord.TryGetValue((nextX, y), out var next)) break;
                AddTrafficDemand(world, districtsByCoord[(x, y)].Id, next.Id);
                x = nextX;
            }
            while (y != to.GridY)
            {
                var nextY = y + Math.Sign(to.GridY - y);
                if (!districtsByCoord.TryGetValue((x, nextY), out var next)) break;
                AddTrafficDemand(world, districtsByCoord[(x, y)].Id, next.Id);
                y = nextY;
            }
        }

        foreach (var link in world.TrafficLinks)
            link.Congestion = decimal.Round(Clamp((decimal)link.DailyDemand / Math.Max(1, link.Capacity), 0m, 2.5m), 3);

        var citizensById = State.Citizens.Where(c => c.Alive).ToDictionary(c => c.Id);
        foreach (var vehicle in world.Vehicles.Values)
        {
            if (!citizensById.TryGetValue(vehicle.OwnerCitizenId, out var owner)) continue;
            vehicle.CurrentDistrictId = owner.DistrictId;
            vehicle.Status = owner.CurrentActivity == "Deslocando-se" ? "Em rota" : "Estacionado";
            vehicle.TargetDistrictId = owner.EmployedCompanyId is int companyId &&
                                       companies.TryGetValue(companyId, out var employer)
                ? employer.DistrictId
                : owner.DistrictId;
        }
    }

    private static void AddTrafficDemand(AaaWorldState world, int a, int b)
    {
        var link = world.TrafficLinks.FirstOrDefault(x =>
            (x.FromDistrictId == a && x.ToDistrictId == b) ||
            (x.FromDistrictId == b && x.ToDistrictId == a));
        if (link is not null) link.DailyDemand++;
    }

    private void UpdateRegionAggregates()
    {
        var world = State.Aaa;
        var citizenById = State.Citizens.Where(c => c.Alive).ToDictionary(c => c.Id);
        foreach (var region in world.Regions.Values)
        {
            var regionCitizens = world.Citizens.Values
                .Where(p => citizenById.TryGetValue(p.CitizenId, out var c) && c.DistrictId == region.DistrictId)
                .ToArray();
            region.Population = regionCitizens.Length;
            region.StatisticalCitizens = regionCitizens.Count(x => x.Lod == SimulationLodTier.Statistical);
            region.RegionalCitizens = regionCitizens.Count(x => x.Lod == SimulationLodTier.Regional);
            region.ActiveCitizens = regionCitizens.Count(x => x.Lod == SimulationLodTier.Active);
            region.InteractiveCitizens = regionCitizens.Count(x => x.Lod == SimulationLodTier.Interactive);
            region.Companies = State.Companies.Count(c => c.Open && c.DistrictId == region.DistrictId);
            region.Vehicles = world.Vehicles.Values.Count(v => v.CurrentDistrictId == region.DistrictId);
            var traffic = world.TrafficLinks
                .Where(x => x.FromDistrictId == region.DistrictId || x.ToDistrictId == region.DistrictId)
                .Select(x => x.Congestion).ToArray();
            var congestion = traffic.Length == 0 ? 0m : traffic.Average();
            region.ActivityIndex = decimal.Round(Clamp(region.Population / 250m + region.Companies / 60m + congestion * 0.30m, 0m, 2m), 3);
        }
    }
}

public static class AaaSimulationValidator
{
    public static void Validate(GameState state)
    {
        var world = state.Aaa;
        if (!world.Initialized) return;
        if (world.Version != AaaWorldState.CurrentVersion)
            throw new InvalidDataException($"Versão da simulação AAA incompatível: {world.Version}.");
        if (world.Regions.Count != state.Districts.Count)
            throw new InvalidDataException("Regiões AAA não correspondem aos distritos.");
        if (world.Citizens.Values.Count(x => x.Lod == SimulationLodTier.Interactive) > world.Performance.InteractiveCitizenCap)
            throw new InvalidDataException("Mais cidadãos interativos que o orçamento permitido.");
        if (world.TrafficLinks.Any(x => x.Capacity <= 0 || x.DailyDemand < 0 || x.Congestion is < 0m or > 2.5m))
            throw new InvalidDataException("Estado de tráfego inválido.");

        foreach (var profile in world.Citizens.Values)
        {
            var n = profile.Needs;
            var values = new[] { n.Hunger, n.Thirst, n.Fatigue, n.Hygiene, n.Bathroom, n.Social, n.Fun, n.Comfort, n.Safety, n.Health };
            if (values.Any(v => v is < 0m or > 100m))
                throw new InvalidDataException($"Necessidade fora do intervalo no cidadão {profile.CitizenId}.");
            if (profile.Memories.Count > 64)
                throw new InvalidDataException($"Memória acima do limite no cidadão {profile.CitizenId}.");
        }
    }
}
