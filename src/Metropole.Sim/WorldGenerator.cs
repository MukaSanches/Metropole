namespace Metropole.Sim;

public static class WorldGenerator
{
    private static readonly string[] DistrictNames =
    [
        "Centro", "Jardins", "Vila Nova", "Parque Norte", "Estação", "Bela Vista", "Industrial", "Lago Sul", "Horizonte"
    ];

    private static readonly string[] FirstNames =
    [
        "Ana", "Bruno", "Caio", "Davi", "Elisa", "Fabio", "Gabriela", "Hugo", "Iara", "João",
        "Karen", "Lucas", "Marina", "Nicolas", "Olivia", "Paulo", "Rafaela", "Sergio", "Talita", "Vinicius",
        "Aline", "Bianca", "Cesar", "Diego", "Eduarda", "Felipe", "Giovana", "Henrique", "Isabela", "Leandro"
    ];

    private static readonly string[] LastNames =
    [
        "Almeida", "Barbosa", "Cardoso", "Dias", "Esteves", "Ferreira", "Gomes", "Haddad", "Ishikawa", "Jardim",
        "Klein", "Lima", "Moraes", "Nunes", "Oliveira", "Pereira", "Queiroz", "Rocha", "Silva", "Teixeira",
        "Uchoa", "Vieira", "Watanabe", "Xavier", "Yamada", "Zanetti"
    ];

    private static readonly string[] CompanyPrefixes =
    [
        "Aurora", "Atlas", "Central", "Cívica", "Delta", "Eixo", "Horizonte", "Íntegra", "Norte", "Nova",
        "Ponte", "Prisma", "Rota", "Vértice", "Urbana", "Vale", "Metrópole", "Pioneira", "Lumen", "Marco"
    ];

    public static GameState Generate(long seed, string playerName)
    {
        var rng = new DeterministicRng(seed);
        var state = new GameState
        {
            Seed = seed,
            CurrentDay = 0,
            Treasury = 50_000_000m,
            Player = new PlayerState
            {
                Name = string.IsNullOrWhiteSpace(playerName) ? "Cidadão" : playerName.Trim(),
                DistrictId = 5,
                Cash = 2_500m
            }
        };

        for (var i = 0; i < DistrictNames.Length; i++)
        {
            state.Districts.Add(new DistrictState
            {
                Id = i + 1,
                Name = DistrictNames[i],
                GridX = i % 3,
                GridY = i / 3,
                RentIndex = rng.NextDecimal(0.75m, 1.55m),
                WealthIndex = rng.NextDecimal(0.70m, 1.60m),
                LogisticsIndex = rng.NextDecimal(0.80m, 1.35m),
                SocialIndex = rng.NextDecimal(0.75m, 1.35m),
                SafetyIndex = rng.NextDecimal(0.72m, 1.38m)
            });
        }

        foreach (var sector in ContentCatalog.Sectors)
        {
            state.Markets.Add(new ProductMarketState
            {
                Family = sector.ProductFamily,
                BasePrice = sector.BasePrice,
                Price = sector.BasePrice * rng.NextDecimal(0.92m, 1.08m),
                Stock = rng.NextDecimal(800m, 2_400m)
            });
        }

        for (var i = 0; i < 180; i++)
        {
            var archetype = rng.Pick(ContentCatalog.Businesses);
            var sector = ContentCatalog.Sectors.First(s => s.Name == archetype.Sector);
            var company = new CompanyState
            {
                Id = state.NextCompanyId++,
                Name = $"{rng.Pick(CompanyPrefixes)} {sector.Name} {i + 1:000}",
                Sector = sector.Name,
                ProductFamily = sector.ProductFamily,
                Archetype = archetype.Format,
                DistrictId = rng.NextInt(1, 10),
                Cash = rng.NextDecimal(30_000m, 220_000m),
                BaseWage = rng.NextDecimal(95m, 230m),
                Reputation = rng.NextDecimal(0.35m, 0.85m),
                BrandAwareness = rng.NextDecimal(0.08m, 0.42m),
                CustomerLoyalty = rng.NextDecimal(0.12m, 0.48m),
                ProductQuality = rng.NextDecimal(0.38m, 0.78m),
                Innovation = rng.NextDecimal(0.20m, 0.72m),
                EmployeeMorale = rng.NextDecimal(0.48m, 0.78m),
                PriceMultiplier = rng.NextDecimal(0.88m, 1.14m),
                MarketingBudgetDaily = rng.NextDecimal(12m, 75m),
                Strategy = StrategyFor(i),
                Productivity = sector.Productivity * rng.NextDecimal(0.80m, 1.25m),
                DesiredEmployees = rng.NextInt(4, 18)
            };
            company.BrandName = company.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            company.Slogan = "Valor que move a cidade.";
            state.Companies.Add(company);
        }

        for (var i = 0; i < 1_200; i++)
        {
            var ageYears = rng.NextInt(0, 91);
            state.Citizens.Add(new CitizenState
            {
                Id = state.NextCitizenId++,
                Name = $"{rng.Pick(FirstNames)} {rng.Pick(LastNames)}",
                AgeDays = (ageYears * 365) + rng.NextInt(0, 365),
                Cash = ageYears >= 18 ? rng.NextDecimal(600m, 18_000m) : 0m,
                DistrictId = rng.NextInt(1, 10),
                HouseholdId = rng.NextInt(1, 430),
                SkillTier = rng.NextInt(1, 6),
                EducationLevel = ageYears < 6 ? 0 : ageYears < 18 ? 1 : rng.NextInt(1, 5),
                Hunger = rng.NextDecimal(5m, 30m),
                Energy = rng.NextDecimal(55m, 95m),
                Happiness = rng.NextDecimal(42m, 78m),
                Stress = rng.NextDecimal(8m, 45m),
                Ambition = rng.NextDecimal(0.15m, 0.95m),
                Sociability = rng.NextDecimal(0.15m, 0.95m),
                Discipline = rng.NextDecimal(0.15m, 0.95m),
                RiskTolerance = rng.NextDecimal(0.15m, 0.95m)
            });
        }

        foreach (var citizen in state.Citizens.Where(c => c.AgeYears is >= 18 and < 66))
        {
            if (!rng.Chance(0.72)) continue;
            var candidates = state.Companies
                .Where(c => c.Open && c.EmployeeIds.Count < c.DesiredEmployees)
                .ToArray();
            if (candidates.Length == 0) break;
            var company = rng.Pick(candidates);
            citizen.EmployedCompanyId = company.Id;
            citizen.DailyWage = decimal.Round(company.BaseWage * (0.72m + (citizen.SkillTier * 0.08m)), 2);
            company.EmployeeIds.Add(citizen.Id);
        }

        state.History.Add(new HistoryEvent
        {
            Day = 0,
            Kind = "Mundo",
            Summary = "A cidade foi gerada e iniciou sua atividade econômica.",
            Cause = $"seed {seed} + regras {state.RulesVersion}"
        });

        SystemicBootstrap.Ensure(state);
        SimulationValidator.Validate(state);
        return state;
    }

    private static string StrategyFor(int index) => (index % 6) switch
    {
        0 => "Equilibrada",
        1 => "Penetração",
        2 => "Premium",
        3 => "Eficiência",
        4 => "Crescimento",
        _ => "Marca forte"
    };
}
