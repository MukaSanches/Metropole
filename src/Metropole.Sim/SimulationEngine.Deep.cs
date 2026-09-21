namespace Metropole.Sim;

public sealed partial class SimulationEngine
{
    private static readonly string[] WeatherStates =
    [
        "Ensolarado", "Parcialmente nublado", "Nublado", "Chuva leve", "Chuva forte", "Neblina"
    ];

    private static readonly string[] StrategyNames =
    [
        "Equilibrada", "Penetração", "Premium", "Eficiência", "Crescimento", "Marca forte"
    ];

    private void ProcessDeepSystems(DeterministicRng rng)
    {
        InitializeExtendedCompanyState();
        UpdateWeather(rng);
        UpdateCitizenLives(rng);
        UpdateCompanyStrategies(rng);
        UpdateMarketSharesAndRivalries();
        UpdateCompanyFinanceHistory();
        RebalanceBusinessPopulation(rng);
        UpdateCityConfidence();
        GenerateNarrativeEvents(rng);
    }

    private void InitializeExtendedCompanyState()
    {
        foreach (var company in State.Companies)
        {
            if (string.IsNullOrWhiteSpace(company.BrandName))
                company.BrandName = CreateBrandName(company);

            if (string.IsNullOrWhiteSpace(company.Slogan))
                company.Slogan = CreateSlogan(company);

            if (string.IsNullOrWhiteSpace(company.Strategy))
                company.Strategy = StrategyNames[company.Id % StrategyNames.Length];

            if (!company.Open)
                company.OperatingStatus = "Encerrada";
        }
    }

    private void UpdateWeather(DeterministicRng rng)
    {
        if (State.CurrentDay == 1 || State.CurrentDay % 2 == 0 || string.IsNullOrWhiteSpace(State.Weather))
        {
            var seasonal = (State.CurrentDay / 90) % 4;
            var roll = rng.NextInt(0, 100);
            State.Weather = roll switch
            {
                < 43 => "Ensolarado",
                < 64 => "Parcialmente nublado",
                < 79 => "Nublado",
                < 91 => "Chuva leve",
                < 97 => "Chuva forte",
                _ => "Neblina"
            };

            var seasonalBase = seasonal switch
            {
                0 => 27m,
                1 => 22m,
                2 => 18m,
                _ => 24m
            };
            State.TemperatureC = decimal.Round(seasonalBase + rng.NextDecimal(-5m, 5m), 1);
        }
    }

    private void UpdateCitizenLives(DeterministicRng rng)
    {
        foreach (var citizen in State.Citizens.Where(c => c.Alive))
        {
            var employed = citizen.EmployedCompanyId is not null;
            var company = employed
                ? State.Companies.FirstOrDefault(c => c.Id == citizen.EmployedCompanyId)
                : null;

            citizen.Energy = Clamp(citizen.Energy + (employed ? 5m : 8m), 0m, 100m);
            citizen.Stress = Clamp(citizen.Stress + (employed ? 0.7m : 0.3m), 0m, 100m);

            if (company is not null)
            {
                citizen.Happiness = Clamp(
                    citizen.Happiness
                    + (company.EmployeeMorale - 0.5m) * 1.5m
                    + (company.Reputation - 0.5m) * 0.4m,
                    0m, 100m);
            }
            else if (citizen.AgeYears is >= 18 and < 66)
            {
                citizen.Happiness = Clamp(citizen.Happiness - 0.35m, 0m, 100m);
                citizen.Stress = Clamp(citizen.Stress + 0.8m, 0m, 100m);
            }

            if (citizen.Cash < 200m)
                citizen.Stress = Clamp(citizen.Stress + 0.8m, 0m, 100m);
            else if (citizen.Cash > 5_000m)
                citizen.Stress = Clamp(citizen.Stress - 0.3m, 0m, 100m);

            if (citizen.AgeYears is >= 6 and <= 22)
            {
                if (citizen.AgeYears >= 18 && citizen.Ambition > 0.55m && State.CurrentDay % 180 == 0)
                    citizen.EducationLevel = Math.Min(4, citizen.EducationLevel + 1);
                else if (citizen.AgeYears < 18)
                    citizen.EducationLevel = Math.Max(citizen.EducationLevel, 1);
            }
        }

        ProcessRelationships(rng);
        ProcessCareerMobility(rng);
        UpdatePlayerWellbeing();
    }

    private void ProcessRelationships(DeterministicRng rng)
    {
        if (State.CurrentDay % 30 != 0) return;

        var singles = State.Citizens
            .Where(c => c.Alive && c.AgeYears is >= 20 and <= 55 && c.PartnerCitizenId is null)
            .OrderBy(c => c.Id)
            .ToArray();

        var matches = Math.Min(5, singles.Length / 2);
        for (var i = 0; i < matches; i++)
        {
            var a = singles[rng.NextInt(0, singles.Length)];
            if (a.PartnerCitizenId is not null) continue;

            var candidates = singles
                .Where(c => c.Id != a.Id
                            && c.PartnerCitizenId is null
                            && c.DistrictId == a.DistrictId
                            && Math.Abs(c.AgeYears - a.AgeYears) <= 12)
                .ToArray();

            if (candidates.Length == 0) continue;
            var b = candidates[rng.NextInt(0, candidates.Length)];

            var compatibility =
                1m - Math.Abs(a.Sociability - b.Sociability) * 0.35m
                - Math.Abs(a.Ambition - b.Ambition) * 0.20m
                - Math.Abs(a.Discipline - b.Discipline) * 0.15m;

            if (compatibility < 0.55m || !rng.Chance((double)Math.Clamp(compatibility * 0.5m, 0.10m, 0.65m)))
                continue;

            a.PartnerCitizenId = b.Id;
            b.PartnerCitizenId = a.Id;
            a.Happiness = Clamp(a.Happiness + 8m, 0m, 100m);
            b.Happiness = Clamp(b.Happiness + 8m, 0m, 100m);

            AddHistory("Relacionamento", $"{a.Name} e {b.Name} começaram um relacionamento.",
                "proximidade + compatibilidade de personalidade → vínculo social");
        }
    }

    private void ProcessCareerMobility(DeterministicRng rng)
    {
        if (State.CurrentDay % 7 != 0) return;

        var switches = 0;
        foreach (var citizen in State.Citizens
                     .Where(c => c.Alive && c.AgeYears is >= 18 and < 66 && c.EmployedCompanyId is not null)
                     .OrderBy(c => c.Id))
        {
            if (switches >= 10) break;
            if (State.CurrentDay - citizen.LastCareerChangeDay < 30) continue;

            var current = State.Companies.FirstOrDefault(c => c.Id == citizen.EmployedCompanyId && c.Open);
            if (current is null) continue;

            var dissatisfaction =
                (1m - current.EmployeeMorale) * 0.45m
                + citizen.Stress / 100m * 0.25m
                + citizen.Ambition * 0.20m;

            if (!rng.Chance((double)Math.Clamp(dissatisfaction * 0.18m, 0.01m, 0.25m))) continue;

            var next = State.Companies
                .Where(c => c.Open
                            && c.Id != current.Id
                            && c.EmployeeIds.Count < c.DesiredEmployees
                            && c.BaseWage > current.BaseWage * 1.07m)
                .OrderByDescending(c => c.BaseWage * (0.7m + c.Reputation * 0.3m))
                .FirstOrDefault();

            if (next is null) continue;

            current.EmployeeIds.Remove(citizen.Id);
            next.EmployeeIds.Add(citizen.Id);
            citizen.EmployedCompanyId = next.Id;
            citizen.DailyWage = decimal.Round(next.BaseWage * (0.72m + citizen.SkillTier * 0.08m), 2);
            citizen.LastCareerChangeDay = State.CurrentDay;
            citizen.Happiness = Clamp(citizen.Happiness + 4m, 0m, 100m);
            switches++;

            AddHistory("Carreira", $"{citizen.Name} trocou {current.Name} por {next.Name}.",
                "salário melhor + insatisfação no emprego anterior → mobilidade profissional", next.Id);
        }
    }

    private void UpdatePlayerWellbeing()
    {
        var p = State.Player;
        var district = State.Districts.FirstOrDefault(d => d.Id == p.DistrictId);
        var employedBonus = p.EmployerCompanyId is null ? -0.25m : 0.20m;
        var moneyPressure = p.Cash < 400m ? 1.2m : p.Cash < 1_500m ? 0.4m : -0.15m;
        var healthPressure = (100m - p.Health) / 100m;

        p.Stress = Clamp(p.Stress + moneyPressure + healthPressure * 0.6m - p.Social / 100m * 0.25m, 0m, 100m);
        p.Happiness = Clamp(
            p.Happiness
            + employedBonus
            + ((district?.SocialIndex ?? 1m) - 1m) * 0.30m
            + (p.Social - 50m) / 100m * 0.35m
            - p.Stress / 100m * 0.30m,
            0m, 100m);
        p.Health = Clamp(p.Health + (p.Fitness - 50m) / 100m * 0.15m - p.Stress / 100m * 0.08m, 0m, 100m);
    }

    private void UpdateCompanyStrategies(DeterministicRng rng)
    {
        var sectorAverages = State.Companies
            .Where(c => c.Open)
            .GroupBy(c => c.ProductFamily)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Wage = g.Average(c => c.BaseWage),
                    Margin = g.Average(c => c.LastRevenue - c.LastCosts)
                });

        foreach (var company in State.Companies)
        {
            company.AgeDays++;
            if (!company.Open)
            {
                company.OperatingStatus = "Encerrada";
                continue;
            }

            var profit = company.LastRevenue - company.LastCosts;
            var creditLimit = CreditLimit(company);
            var debtRatio = creditLimit <= 0m ? 0m : company.Debt / creditLimit;

            company.OperatingStatus =
                company.LossDays > 50 || debtRatio > 0.82m ? "Crise" :
                company.LossDays > 22 || debtRatio > 0.58m ? "Atenção" :
                "Ativa";

            var awarenessGain = Math.Min(0.025m, company.LastMarketing / Math.Max(1m, company.Cash + company.LastMarketing) * 6m);
            company.BrandAwareness = Clamp01(company.BrandAwareness * 0.997m + awarenessGain);

            var loyaltyTarget =
                company.ProductQuality * 0.42m
                + company.Reputation * 0.28m
                + company.EmployeeMorale * 0.12m
                + (2m - Clamp(company.PriceMultiplier, 0.7m, 1.4m)) * 0.09m;
            company.CustomerLoyalty = Clamp01(company.CustomerLoyalty * 0.985m + Clamp01(loyaltyTarget) * 0.015m);

            var sector = sectorAverages.GetValueOrDefault(company.ProductFamily);
            var wageRelative = sector is null || sector.Wage <= 0m ? 1m : company.BaseWage / sector.Wage;
            var moraleTarget =
                0.45m
                + Clamp(wageRelative - 1m, -0.35m, 0.35m) * 0.45m
                + (profit >= 0m ? 0.08m : -0.08m)
                - Math.Min(0.18m, company.LossDays * 0.003m);
            company.EmployeeMorale = Clamp01(company.EmployeeMorale * 0.94m + Clamp01(moraleTarget) * 0.06m);

            var reputationTarget =
                company.ProductQuality * 0.38m
                + company.CustomerLoyalty * 0.24m
                + company.EmployeeMorale * 0.18m
                + company.BrandAwareness * 0.20m;
            company.Reputation = Clamp01(company.Reputation * 0.985m + reputationTarget * 0.015m);

            if (profit > 0m)
                company.Innovation = Clamp01(company.Innovation + Math.Min(0.0015m, profit / 1_000_000m));
            else
                company.Innovation = Clamp01(company.Innovation * 0.999m);

            if (company.PlayerOwned) continue;
            if (State.CurrentDay % 7 != 0) continue;

            if (company.MarketShare < 0.035m || company.LossDays > 12)
            {
                company.Strategy = "Penetração";
                company.PriceMultiplier = Clamp(company.PriceMultiplier - 0.025m, 0.82m, 1.18m);
                company.MarketingBudgetDaily = Clamp(company.MarketingBudgetDaily + 6m, 5m, 220m);
            }
            else if (company.ProductQuality > 0.72m && company.Reputation > 0.65m)
            {
                company.Strategy = "Premium";
                company.PriceMultiplier = Clamp(company.PriceMultiplier + 0.018m, 0.88m, 1.28m);
                company.MarketingBudgetDaily = Clamp(company.MarketingBudgetDaily + 3m, 5m, 250m);
            }
            else if (profit < 0m)
            {
                company.Strategy = "Eficiência";
                company.PriceMultiplier = Clamp(company.PriceMultiplier - 0.010m, 0.86m, 1.18m);
                company.MarketingBudgetDaily = Clamp(company.MarketingBudgetDaily - 5m, 5m, 180m);
            }
            else
            {
                company.Strategy = rng.Chance(0.5) ? "Crescimento" : "Equilibrada";
                company.MarketingBudgetDaily = Clamp(company.MarketingBudgetDaily + rng.NextDecimal(-2m, 3m), 5m, 180m);
            }
        }
    }

    private void UpdateMarketSharesAndRivalries()
    {
        foreach (var familyGroup in State.Companies.Where(c => c.Open).GroupBy(c => c.ProductFamily))
        {
            var companies = familyGroup.ToArray();
            var totalRevenue = companies.Sum(c => Math.Max(0m, c.LastRevenue));
            if (totalRevenue <= 0m)
            {
                var even = companies.Length == 0 ? 0m : 1m / companies.Length;
                foreach (var company in companies) company.MarketShare = even;
            }
            else
            {
                foreach (var company in companies)
                    company.MarketShare = Clamp01(Math.Max(0m, company.LastRevenue) / totalRevenue);
            }

            foreach (var company in companies)
            {
                var rival = companies
                    .Where(c => c.Id != company.Id)
                    .OrderByDescending(c => c.MarketShare)
                    .ThenByDescending(c => c.BrandAwareness)
                    .FirstOrDefault();

                company.RivalCompanyId = rival?.Id;
                if (rival is null)
                {
                    company.RivalryIntensity = 0m;
                    continue;
                }

                var shareCloseness = 1m - Math.Min(1m, Math.Abs(company.MarketShare - rival.MarketShare) * 4m);
                var brandOverlap = 1m - Math.Min(1m, Math.Abs(company.BrandAwareness - rival.BrandAwareness));
                company.RivalryIntensity = Clamp01(shareCloseness * 0.65m + brandOverlap * 0.35m);
            }
        }
    }

    private void UpdateCompanyFinanceHistory()
    {
        foreach (var company in State.Companies)
        {
            company.FinanceHistory ??= [];
            company.FinanceHistory.Add(new CompanyFinanceSnapshot
            {
                Day = State.CurrentDay,
                Revenue = company.LastRevenue,
                Costs = company.LastCosts,
                Profit = company.LastRevenue - company.LastCosts,
                Cash = company.Cash,
                Debt = company.Debt,
                MarketShare = company.MarketShare
            });

            if (company.FinanceHistory.Count > 90)
                company.FinanceHistory.RemoveRange(0, company.FinanceHistory.Count - 90);
        }
    }

    private void RebalanceBusinessPopulation(DeterministicRng rng)
    {
        if (State.CurrentDay % 30 != 0) return;

        var target = Math.Clamp(State.Population / 8, 110, 175);
        var missing = target - State.OpenCompanies;
        if (missing <= 0) return;

        var creations = Math.Min(8, Math.Max(1, missing / 8));
        for (var i = 0; i < creations; i++)
        {
            if (State.Treasury < 25_000m) break;

            var market = State.Markets
                .OrderByDescending(m => (m.DailyDemand + 1m) / Math.Max(1m, m.Stock))
                .ThenBy(m => m.Stock)
                .First();
            var sector = ContentCatalog.Sectors.First(s => s.ProductFamily == market.Family);
            var district = State.Districts[rng.NextInt(0, State.Districts.Count)];
            var capital = Math.Min(35_000m, State.Treasury * 0.001m);
            capital = Math.Max(8_000m, capital);

            if (State.Treasury < capital) break;
            State.Treasury -= capital;

            var id = State.NextCompanyId++;
            var company = new CompanyState
            {
                Id = id,
                Name = $"Nova {sector.Name} {id:000}",
                BrandName = $"{sector.Name.Split(' ')[0]} {id:00}",
                Slogan = CreateSloganForSector(sector.Name),
                Sector = sector.Name,
                ProductFamily = sector.ProductFamily,
                Archetype = "Empresa emergente",
                Strategy = rng.Pick(StrategyNames),
                DistrictId = district.Id,
                Cash = capital,
                BaseWage = rng.NextDecimal(90m, 155m),
                Reputation = rng.NextDecimal(0.32m, 0.50m),
                BrandAwareness = rng.NextDecimal(0.08m, 0.22m),
                CustomerLoyalty = rng.NextDecimal(0.10m, 0.28m),
                ProductQuality = rng.NextDecimal(0.40m, 0.62m),
                Innovation = rng.NextDecimal(0.25m, 0.55m),
                EmployeeMorale = rng.NextDecimal(0.48m, 0.72m),
                Productivity = sector.Productivity * rng.NextDecimal(0.85m, 1.12m),
                PriceMultiplier = rng.NextDecimal(0.92m, 1.10m),
                MarketingBudgetDaily = rng.NextDecimal(15m, 55m),
                DesiredEmployees = rng.NextInt(3, 9)
            };
            State.Companies.Add(company);

            AddHistory("Nova empresa", $"{company.BrandName} abriu no bairro {district.Name}.",
                "demanda local + capital disponível → nova concorrente", company.Id);
        }
    }

    private void UpdateCityConfidence()
    {
        var open = State.Companies.Where(c => c.Open).ToArray();
        if (open.Length == 0)
        {
            State.CityConfidence = 0m;
            return;
        }

        var avgMorale = open.Average(c => c.EmployeeMorale);
        var avgReputation = open.Average(c => c.Reputation);
        var employment = 1m - State.UnemploymentRate;
        State.CityConfidence = Clamp01(avgMorale * 0.30m + avgReputation * 0.25m + employment * 0.45m);
    }

    private void GenerateNarrativeEvents(DeterministicRng rng)
    {
        if (State.CurrentDay % 7 != 0) return;

        var playerCompany = State.Player.BusinessCompanyId is int companyId
            ? State.Companies.FirstOrDefault(c => c.Id == companyId && c.Open)
            : null;

        if (playerCompany?.RivalCompanyId is int rivalId)
        {
            var rival = State.Companies.FirstOrDefault(c => c.Id == rivalId && c.Open);
            if (rival is not null && playerCompany.RivalryIntensity > 0.62m)
            {
                AddHistory(
                    "Rivalidade",
                    $"{rival.BrandName} pressiona {playerCompany.BrandName} no mercado de {playerCompany.ProductFamily}.",
                    $"participações próximas + disputa de marca → rivalidade {playerCompany.RivalryIntensity:P0}",
                    rival.Id);
            }
        }

        if (rng.Chance(0.20))
        {
            var notable = State.Companies
                .Where(c => c.Open)
                .OrderByDescending(c => c.LastRevenue)
                .FirstOrDefault();

            if (notable is not null)
            {
                AddHistory(
                    "Notícia",
                    $"{notable.BrandName} liderou o faturamento diário em {notable.ProductFamily}.",
                    $"receita Cr$ {notable.LastRevenue:N0} + marca {notable.BrandAwareness:P0}",
                    notable.Id);
            }
        }
    }

    private void ProcessInstitutionalDemand(DeterministicRng rng)
    {
        if (State.Treasury < 100_000m) return;

        var budget = Math.Min(55_000m, State.Treasury * 0.0009m);
        if (budget <= 0m) return;

        var markets = State.Markets.OrderBy(m => m.Family).ToArray();
        var perMarket = budget / Math.Max(1, markets.Length);

        foreach (var market in markets)
        {
            var sellers = State.Companies
                .Where(c => c.Open && c.ProductFamily == market.Family)
                .OrderByDescending(c => c.ProductQuality * 0.35m + c.Reputation * 0.25m + c.BrandAwareness * 0.25m + c.Innovation * 0.15m)
                .Take(3)
                .ToArray();

            if (sellers.Length == 0 || market.Stock <= 0m) continue;

            var remaining = perMarket;
            foreach (var seller in sellers)
            {
                if (remaining <= 0m || State.Treasury <= 0m) break;
                var price = decimal.Round(market.Price * Clamp(seller.PriceMultiplier, 0.75m, 1.40m), 2);
                var spend = Math.Min(remaining / sellers.Length, Math.Min(State.Treasury, price * Math.Max(1m, Math.Min(30m, market.Stock))));
                if (spend <= 0m) continue;

                var units = Math.Min(market.Stock, spend / Math.Max(1m, price));
                var actual = decimal.Round(units * price, 2);
                if (actual <= 0m) continue;

                State.Treasury -= actual;
                seller.Cash += actual;
                seller.LastRevenue += actual;
                market.Stock -= units;
                market.DailyDemand += units;
                remaining -= actual;
            }
        }
    }

    private CompanyState SelectCompetitiveSeller(
        CompanyState[] sellers,
        CitizenState citizen,
        ProductMarketState market,
        DeterministicRng rng)
    {
        CompanyState? best = null;
        decimal bestScore = decimal.MinValue;

        foreach (var seller in sellers)
        {
            var price = Clamp(seller.PriceMultiplier, 0.72m, 1.45m);
            var priceAttractiveness = Clamp(1.25m / price, 0.55m, 1.45m);
            var proximity = seller.DistrictId == citizen.DistrictId ? 0.28m : 0m;
            var personalityFit = citizen.RiskTolerance > 0.62m
                ? seller.Innovation * 0.12m
                : seller.Reputation * 0.12m;
            var jitter = rng.NextDecimal(0m, 0.12m);

            var score =
                seller.BrandAwareness * 0.90m
                + seller.ProductQuality * 1.10m
                + seller.Reputation * 0.80m
                + seller.CustomerLoyalty * 0.55m
                + priceAttractiveness * 0.75m
                + proximity
                + personalityFit
                + jitter;

            if (score <= bestScore) continue;
            bestScore = score;
            best = seller;
        }

        return best ?? sellers[0];
    }

    private static string CreateBrandName(CompanyState company)
    {
        var first = company.Name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "Metrópole";
        return first.Length <= 18 ? first : first[..18];
    }

    private static string CreateSlogan(CompanyState company) => CreateSloganForSector(company.Sector);

    private static string CreateSloganForSector(string sector)
    {
        if (sector.Contains("Aliment", StringComparison.OrdinalIgnoreCase)) return "Sabor que aproxima.";
        if (sector.Contains("Tecn", StringComparison.OrdinalIgnoreCase)) return "Ideias que viram futuro.";
        if (sector.Contains("Saúde", StringComparison.OrdinalIgnoreCase)) return "Cuidado em cada escolha.";
        if (sector.Contains("Educa", StringComparison.OrdinalIgnoreCase)) return "Conhecimento que transforma.";
        if (sector.Contains("Constru", StringComparison.OrdinalIgnoreCase)) return "Construindo confiança.";
        if (sector.Contains("Log", StringComparison.OrdinalIgnoreCase)) return "Chegar melhor. Chegar sempre.";
        if (sector.Contains("Comér", StringComparison.OrdinalIgnoreCase)) return "Escolhas para o seu dia.";
        return "Valor que move a cidade.";
    }

    private static decimal Clamp01(decimal value) => Math.Clamp(value, 0m, 1m);
}
