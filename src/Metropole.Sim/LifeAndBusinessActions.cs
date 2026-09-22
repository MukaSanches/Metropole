namespace Metropole.Sim;

public static class LifeActions
{
    public static void AdvanceHours(this SimulationEngine engine, int hours)
    {
        if (hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
        for (var i = 0; i < hours; i++)
            AdvanceOneHour(engine, null);
    }

    public static void WorkShift(this SimulationEngine engine, int hours = 8)
    {
        if (hours <= 0) return;
        var state = engine.State;
        if (state.Player.EmployerCompanyId is null) return;

        for (var i = 0; i < hours; i++)
            AdvanceOneHour(engine, "Trabalhando");

        state.Player.CareerReputation = Math.Clamp(state.Player.CareerReputation + hours * 0.0015m, 0m, 1m);
    }

    public static void Sleep(this SimulationEngine engine, int hours = 8)
    {
        if (hours <= 0) return;
        for (var i = 0; i < hours; i++)
            AdvanceOneHour(engine, "Dormindo");
    }

    public static bool Study(this SimulationEngine engine, int hours = 4)
    {
        if (hours <= 0) return false;
        var state = engine.State;
        var cost = decimal.Round(hours * (8m + state.Player.EducationLevel * 3m), 2);
        if (state.Player.Cash < cost) return false;

        state.Player.Cash -= cost;
        state.Treasury += cost;
        for (var i = 0; i < hours; i++)
            AdvanceOneHour(engine, "Estudando");

        state.Player.EducationProgress += hours * 0.035m;
        while (state.Player.EducationProgress >= 1m && state.Player.EducationLevel < 5)
        {
            state.Player.EducationProgress -= 1m;
            state.Player.EducationLevel++;
            state.Player.CareerReputation = Math.Clamp(state.Player.CareerReputation + 0.04m, 0m, 1m);
            state.History.Add(new HistoryEvent
            {
                Day = state.CurrentDay,
                Kind = "Educação",
                Summary = $"{state.Player.Name} alcançou o nível educacional {state.Player.EducationLevel}.",
                Cause = "horas de estudo + investimento em formação → qualificação"
            });
        }
        return true;
    }

    public static void Socialize(this SimulationEngine engine, int hours = 3)
    {
        if (hours <= 0) return;
        for (var i = 0; i < hours; i++)
            AdvanceOneHour(engine, "Socializando");

        var p = engine.State.Player;
        p.Social = Math.Clamp(p.Social + hours * 4m, 0m, 100m);
        p.Happiness = Math.Clamp(p.Happiness + hours * 1.8m, 0m, 100m);
        p.Stress = Math.Clamp(p.Stress - hours * 1.6m, 0m, 100m);

        // Socializar melhora a necessidade social, mas relações românticas só avançam
        // por interações explícitas com um CitizenState real em SocialActions.
    }

    public static void Exercise(this SimulationEngine engine, int hours = 2)
    {
        if (hours <= 0) return;
        for (var i = 0; i < hours; i++)
            AdvanceOneHour(engine, "Exercitando-se");

        var p = engine.State.Player;
        p.Fitness = Math.Clamp(p.Fitness + hours * 2.8m, 0m, 100m);
        p.Health = Math.Clamp(p.Health + hours * 1.2m, 0m, 100m);
        p.Stress = Math.Clamp(p.Stress - hours * 0.8m, 0m, 100m);
    }

    private static void AdvanceOneHour(SimulationEngine engine, string? forcedPlayerActivity)
    {
        var state = engine.State;
        state.CurrentHour = (state.CurrentHour + 1) % 24;

        UpdatePlayerHour(state, forcedPlayerActivity);
        UpdateCitizenHour(state);
        engine.AdvanceAaaHourlyTick();

        if (state.CurrentHour == 0)
            engine.AdvanceOneDay();
    }

    private static void UpdatePlayerHour(GameState state, string? forcedActivity)
    {
        var p = state.Player;
        var activity = forcedActivity ?? ResolvePlayerActivity(state);
        p.CurrentActivity = activity;

        p.Hunger = Math.Clamp(p.Hunger + 0.9m, 0m, 100m);
        p.Social = Math.Clamp(p.Social - 0.12m, 0m, 100m);

        switch (activity)
        {
            case "Dormindo":
                p.Energy = Math.Clamp(p.Energy + 5.2m, 0m, 100m);
                p.Stress = Math.Clamp(p.Stress - 1.1m, 0m, 100m);
                p.Health = Math.Clamp(p.Health + 0.12m, 0m, 100m);
                break;

            case "Trabalhando":
                p.WorkedHoursToday = Math.Min(12m, p.WorkedHoursToday + 1m);
                p.Energy = Math.Clamp(p.Energy - 3.0m, 0m, 100m);
                p.Stress = Math.Clamp(p.Stress + 1.15m, 0m, 100m);
                p.Hunger = Math.Clamp(p.Hunger + 0.7m, 0m, 100m);
                break;

            case "Estudando":
                p.Energy = Math.Clamp(p.Energy - 1.6m, 0m, 100m);
                p.Stress = Math.Clamp(p.Stress + 0.45m, 0m, 100m);
                break;

            case "Socializando":
                p.Energy = Math.Clamp(p.Energy - 0.7m, 0m, 100m);
                p.Social = Math.Clamp(p.Social + 2.2m, 0m, 100m);
                p.Happiness = Math.Clamp(p.Happiness + 0.8m, 0m, 100m);
                break;

            case "Exercitando-se":
                p.Energy = Math.Clamp(p.Energy - 2.0m, 0m, 100m);
                p.Fitness = Math.Clamp(p.Fitness + 0.7m, 0m, 100m);
                break;

            default:
                p.Energy = Math.Clamp(p.Energy + 0.25m, 0m, 100m);
                p.Stress = Math.Clamp(p.Stress - 0.10m, 0m, 100m);
                break;
        }

        if (p.Hunger > 85m)
        {
            p.Happiness = Math.Clamp(p.Happiness - 0.7m, 0m, 100m);
            p.Health = Math.Clamp(p.Health - 0.12m, 0m, 100m);
        }
        if (p.Energy < 15m)
            p.Stress = Math.Clamp(p.Stress + 0.8m, 0m, 100m);
    }

    private static string ResolvePlayerActivity(GameState state)
    {
        var hour = state.CurrentHour;
        if (hour is >= 0 and < 7) return "Dormindo";
        if (state.Player.EmployerCompanyId is not null && hour is >= 9 and < 17) return "Trabalhando";
        if (hour is 7 or 8) return "Deslocando-se";
        if (hour is >= 18 and <= 20) return "Tempo livre";
        return "Em casa";
    }

    private static void UpdateCitizenHour(GameState state)
    {
        var hour = state.CurrentHour;

        foreach (var citizen in state.Citizens.Where(c => c.Alive))
        {
            var activity = ResolveCitizenActivity(citizen, hour);
            citizen.CurrentActivity = activity;

            citizen.Hunger = Math.Clamp(citizen.Hunger + 0.55m, 0m, 100m);

            switch (activity)
            {
                case "Dormindo":
                    citizen.Energy = Math.Clamp(citizen.Energy + 4.4m, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress - 0.7m, 0m, 100m);
                    break;

                case "Trabalhando":
                    citizen.Energy = Math.Clamp(citizen.Energy - 2.1m, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress + 0.65m, 0m, 100m);
                    break;

                case "Estudando":
                    citizen.Energy = Math.Clamp(citizen.Energy - 1.1m, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress + 0.25m, 0m, 100m);
                    break;

                case "Lazer":
                    citizen.Happiness = Math.Clamp(citizen.Happiness + 0.35m, 0m, 100m);
                    citizen.Stress = Math.Clamp(citizen.Stress - 0.35m, 0m, 100m);
                    break;
            }
        }
    }

    private static string ResolveCitizenActivity(CitizenState citizen, int hour)
    {
        if (hour is >= 0 and < 6) return "Dormindo";
        if (citizen.AgeYears is >= 6 and <= 22 && hour is >= 8 and < 15) return "Estudando";
        if (citizen.EmployedCompanyId is not null && hour is >= 8 and < 17) return "Trabalhando";
        if (hour is 7 or 17) return "Deslocando-se";
        if (hour is >= 18 and <= 21) return citizen.Sociability > 0.55m ? "Lazer" : "Em casa";
        return "Em casa";
    }
}

public static class BusinessActions
{
    public static bool ConfigureBrand(this SimulationEngine engine, string brandName, string slogan)
    {
        var state = engine.State;
        if (state.Player.BusinessCompanyId is not int companyId) return false;
        var company = state.Companies.FirstOrDefault(c => c.Id == companyId && c.Open && c.PlayerOwned);
        if (company is null) return false;

        brandName = (brandName ?? "").Trim();
        slogan = (slogan ?? "").Trim();
        if (brandName.Length is < 2 or > 32 || slogan.Length > 70) return false;

        const decimal rebrandCost = 300m;
        if (company.Cash < rebrandCost) return false;

        company.Cash -= rebrandCost;
        state.Treasury += rebrandCost;
        company.BrandName = brandName;
        company.Slogan = string.IsNullOrWhiteSpace(slogan) ? "Valor que move a cidade." : slogan;
        company.BrandAwareness = Math.Clamp(company.BrandAwareness + 0.025m, 0m, 1m);
        company.CustomerLoyalty = Math.Clamp(company.CustomerLoyalty - 0.015m, 0m, 1m);

        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Branding",
            Summary = $"{company.Name} apresentou a marca {company.BrandName}.",
            Cause = $"rebranding Cr$ {rebrandCost:0} → nova identidade + campanha de lançamento",
            EntityId = company.Id
        });
        return true;
    }

    public static bool AdjustMarketingBudget(this SimulationEngine engine, decimal delta)
    {
        var company = GetPlayerCompany(engine);
        if (company is null) return false;
        company.MarketingBudgetDaily = Math.Clamp(company.MarketingBudgetDaily + delta, 0m, 500m);
        return true;
    }

    public static bool SetPricingStrategy(this SimulationEngine engine, string strategy)
    {
        var company = GetPlayerCompany(engine);
        if (company is null) return false;

        switch (strategy)
        {
            case "Penetração":
                company.Strategy = "Penetração";
                company.PriceMultiplier = 0.88m;
                break;
            case "Premium":
                company.Strategy = "Premium";
                company.PriceMultiplier = 1.18m;
                break;
            case "Crescimento":
                company.Strategy = "Crescimento";
                company.PriceMultiplier = 0.96m;
                break;
            case "Eficiência":
                company.Strategy = "Eficiência";
                company.PriceMultiplier = 1.02m;
                break;
            default:
                company.Strategy = "Equilibrada";
                company.PriceMultiplier = 1.00m;
                break;
        }
        return true;
    }

    public static bool InvestInQuality(this SimulationEngine engine, decimal amount)
    {
        var state = engine.State;
        var company = GetPlayerCompany(engine);
        if (company is null || amount <= 0m || company.Cash < amount) return false;

        company.Cash -= amount;
        state.Treasury += amount;
        var gain = Math.Min(0.08m, amount / 35_000m);
        company.ProductQuality = Math.Clamp(company.ProductQuality + gain, 0m, 1m);
        company.Reputation = Math.Clamp(company.Reputation + gain * 0.25m, 0m, 1m);
        company.LastCosts += amount;

        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Qualidade",
            Summary = $"{company.BrandName} investiu Cr$ {amount:N0} em qualidade.",
            Cause = "padronização + fornecedores melhores + controle de qualidade → produto superior",
            EntityId = company.Id
        });
        return true;
    }

    public static bool InvestInInnovation(this SimulationEngine engine, decimal amount)
    {
        var state = engine.State;
        var company = GetPlayerCompany(engine);
        if (company is null || amount <= 0m || company.Cash < amount) return false;

        company.Cash -= amount;
        state.Treasury += amount;
        var gain = Math.Min(0.10m, amount / 30_000m);
        company.Innovation = Math.Clamp(company.Innovation + gain, 0m, 1m);
        company.Productivity = Math.Clamp(company.Productivity + gain * 0.05m, 0.50m, 2.50m);
        company.LastCosts += amount;

        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Inovação",
            Summary = $"{company.BrandName} abriu um ciclo de inovação.",
            Cause = $"P&D Cr$ {amount:N0} → inovação + eficiência potencial",
            EntityId = company.Id
        });
        return true;
    }

    public static bool AdjustBaseWage(this SimulationEngine engine, decimal percentDelta)
    {
        var company = GetPlayerCompany(engine);
        if (company is null) return false;

        var factor = 1m + percentDelta;
        if (factor <= 0m) return false;
        company.BaseWage = decimal.Round(Math.Clamp(company.BaseWage * factor, 40m, 2_000m), 2);
        return true;
    }

    private static CompanyState? GetPlayerCompany(SimulationEngine engine)
    {
        var state = engine.State;
        return state.Player.BusinessCompanyId is int companyId
            ? state.Companies.FirstOrDefault(c => c.Id == companyId && c.Open && c.PlayerOwned)
            : null;
    }
}
