namespace Metropole.Sim;

public sealed partial class SimulationEngine
{
    public GameState State { get; }

    public SimulationEngine(GameState state)
    {
        SimulationValidator.Validate(state);
        State = state;
    }

    public void AdvanceDays(int days)
    {
        if (days < 0) throw new ArgumentOutOfRangeException(nameof(days));
        for (var i = 0; i < days; i++) AdvanceOneDay();
    }

    public void AdvanceOneDay()
    {
        State.CurrentDay++;
        var rng = new DeterministicRng(DailySeed(State.Seed, State.CurrentDay));

        AgeAndNeeds(rng);
        ResetMarketCounters();
        ProcessCompaniesAndPayroll(rng);
        ProcessConsumption(rng);
        ProcessProductionAndPrices();
        ProcessEmployment(rng);
        ProcessDemography(rng);
        ProcessCompanyBirths(rng);
        ProcessPlayerSuccession(rng);
        ProcessDeepSystems(rng);
        CompactHistory();

        SimulationValidator.Validate(State);
    }

    public IReadOnlyList<CompanyState> GetJobBoard(int max = 12) =>
        State.Companies
            .Where(c => c.Open && c.EmployeeIds.Count < c.DesiredEmployees)
            .OrderByDescending(c => c.BaseWage)
            .ThenByDescending(c => c.Reputation)
            .Take(max)
            .ToArray();

    public bool AcceptJob(int companyId)
    {
        var company = State.Companies.FirstOrDefault(c => c.Id == companyId && c.Open);
        if (company is null || company.EmployeeIds.Count >= company.DesiredEmployees) return false;

        State.Player.EmployerCompanyId = company.Id;
        State.Player.DailyWage = decimal.Round(company.BaseWage * 0.88m, 2);
        AddHistory("Carreira", $"{State.Player.Name} começou a trabalhar na {company.Name}.",
            $"vaga disponível → contratação; salário diário {State.Player.DailyWage:0.00}", company.Id);
        return true;
    }

    public bool BuyFood()
    {
        var market = State.Markets.First(m => m.Family == "Alimentos");
        if (market.Stock < 1m || State.Player.Cash < market.Price) return false;

        var sellers = State.Companies.Where(c => c.Open && c.ProductFamily == market.Family).OrderByDescending(c => c.Reputation).ToArray();
        if (sellers.Length == 0) return false;

        var seller = sellers[0];
        State.Player.Cash -= market.Price;
        seller.Cash += market.Price;
        seller.LastRevenue += market.Price;
        market.Stock -= 1m;
        market.DailyDemand += 1m;
        State.Player.Hunger = Clamp(State.Player.Hunger - 40m, 0m, 100m);
        AddHistory("Consumo", $"{State.Player.Name} comprou alimentação.",
            $"necessidade → compra → receita para {seller.Name}", seller.Id);
        return true;
    }

    public bool OpenPlayerBusiness(string sectorName)
    {
        if (State.Player.BusinessCompanyId is not null) return false;
        var sector = ContentCatalog.Sectors.FirstOrDefault(s => s.Name.Equals(sectorName, StringComparison.OrdinalIgnoreCase));
        if (sector is null) return false;

        const decimal startupCapital = 5_000m;
        if (State.Player.Cash < startupCapital) return false;

        State.Player.Cash -= startupCapital;
        var seedCredit = Math.Min(3_000m, State.Treasury);
        State.Treasury -= seedCredit;
        var company = new CompanyState
        {
            Id = State.NextCompanyId++,
            Name = $"{State.Player.Name} — {sector.Name}",
            BrandName = State.Player.Name.Length <= 24 ? State.Player.Name : State.Player.Name[..24],
            Slogan = "Começando pequeno. Pensando grande.",
            Sector = sector.Name,
            ProductFamily = sector.ProductFamily,
            Archetype = "Microempresa do jogador",
            Strategy = "Equilibrada",
            OperatingStatus = "Ativa",
            DistrictId = State.Player.DistrictId,
            Cash = startupCapital + seedCredit,
            Debt = seedCredit,
            BaseWage = 120m,
            Reputation = 0.45m,
            BrandAwareness = 0.12m,
            CustomerLoyalty = 0.16m,
            ProductQuality = 0.55m,
            Innovation = 0.38m,
            EmployeeMorale = 0.65m,
            PriceMultiplier = 1.00m,
            MarketingBudgetDaily = 20m,
            Productivity = sector.Productivity,
            DesiredEmployees = 3,
            PlayerOwned = true
        };
        State.Companies.Add(company);
        State.Player.BusinessCompanyId = company.Id;
        AddHistory("Empresa", $"{company.Name} foi fundada.", "capital do jogador → abertura da empresa", company.Id);
        return true;
    }

    public string ExplainCompany(int companyId)
    {
        var c = State.Companies.FirstOrDefault(x => x.Id == companyId);
        if (c is null) return "Empresa não encontrada.";
        if (!c.Open) return $"{c.Name}: encerrada. Causa: {c.ClosureCause}";
        var margin = c.LastRevenue - c.LastCosts;
        return $"{c.Name}: receita {c.LastRevenue:0.00}, custos {c.LastCosts:0.00}, margem {margin:0.00}, caixa {c.Cash:0.00}, dívida {c.Debt:0.00}, perdas consecutivas {c.LossDays}.";
    }

    private void AgeAndNeeds(DeterministicRng rng)
    {
        State.Player.AgeDays++;
        State.Player.Health = Clamp(State.Player.Health + (State.Player.Energy > 45m ? 0.05m : -0.10m), 0m, 100m);

        if (State.CurrentDay % 365 == 0 && State.Player.AgeYears is >= 25 and <= 50 && rng.Chance(0.10))
        {
            State.Player.Children++;
            AddHistory("Família", "Nasceu um descendente na família do jogador.", "ciclo familiar anual");
        }

        foreach (var citizen in State.Citizens.Where(c => c.Alive))
            citizen.AgeDays++;

        ProcessInstitutionalDemand(rng);

        if (State.CurrentDay % 7 == 0)
        {
            var district = State.Districts.First(d => d.Id == State.Player.DistrictId);
            var rent = decimal.Round(35m * district.RentIndex, 2);
            var paid = Math.Min(State.Player.Cash, rent);
            State.Player.Cash -= paid;
            State.Treasury += paid;
        }
    }

    private void ResetMarketCounters()
    {
        foreach (var market in State.Markets)
        {
            market.DailyDemand = 0m;
            market.DailySupply = 0m;
        }
        foreach (var company in State.Companies)
        {
            company.LastRevenue = 0m;
            company.LastCosts = 0m;
            company.LastPayroll = 0m;
            company.LastMarketing = 0m;
            company.LastRent = 0m;
            company.LastOperations = 0m;
            company.LastTaxes = 0m;
        }
    }

    private void ProcessCompaniesAndPayroll(DeterministicRng rng)
    {
        foreach (var company in State.Companies.Where(c => c.Open).ToArray())
        {
            var employees = State.Citizens
                .Where(c => c.Alive && c.EmployedCompanyId == company.Id)
                .ToArray();

            var citizenPayroll = employees.Sum(c => c.DailyWage);
            var playerPayroll = State.Player.EmployerCompanyId == company.Id
                ? decimal.Round(State.Player.DailyWage * Math.Clamp(State.Player.WorkedHoursToday / 8m, 0m, 1m), 2)
                : 0m;
            var payroll = citizenPayroll + playerPayroll;
            var district = CompanyDistrict(company);
            var baseOperations = decimal.Round((18m + employees.Length * 3.0m) * district.LogisticsIndex, 2);
            var rent = decimal.Round((10m + Math.Max(1, company.DesiredEmployees) * 1.8m) * district.RentIndex, 2);
            var marketing = decimal.Round(Math.Max(0m, company.MarketingBudgetDaily), 2);
            var operations = baseOperations + rent + marketing;
            company.LastPayroll = payroll;
            company.LastOperations = baseOperations;
            company.LastRent = rent;
            company.LastMarketing = marketing;
            var required = payroll + operations;

            if (company.Cash < required)
                TryBorrow(company, required - company.Cash);

            if (company.Cash < payroll)
            {
                var layoffsNeeded = Math.Min(employees.Length, Math.Max(1, employees.Length / 3));
                foreach (var worker in employees.OrderBy(c => c.SkillTier).Take(layoffsNeeded))
                {
                    worker.EmployedCompanyId = null;
                    worker.DailyWage = 0m;
                    company.EmployeeIds.Remove(worker.Id);
                }
                AddHistory("Demissão", $"{company.Name} reduziu o quadro em {layoffsNeeded} pessoa(s).",
                    "caixa insuficiente → folha salarial incompatível → demissões", company.Id);
                employees = State.Citizens.Where(c => c.Alive && c.EmployedCompanyId == company.Id).ToArray();
                citizenPayroll = employees.Sum(c => c.DailyWage);
                playerPayroll = State.Player.EmployerCompanyId == company.Id
                ? decimal.Round(State.Player.DailyWage * Math.Clamp(State.Player.WorkedHoursToday / 8m, 0m, 1m), 2)
                : 0m;
                payroll = citizenPayroll + playerPayroll;
            }

            foreach (var employee in employees)
            {
                var pay = Math.Min(company.Cash, employee.DailyWage);
                company.Cash -= pay;
                employee.Cash += pay;
                company.LastCosts += pay;
            }

            if (State.Player.EmployerCompanyId == company.Id && playerPayroll > 0m && company.Cash >= playerPayroll)
            {
                company.Cash -= playerPayroll;
                State.Player.Cash += playerPayroll;
                company.LastCosts += playerPayroll;
                State.Player.CareerExperienceDays++;
            }
            if (State.Player.EmployerCompanyId == company.Id)
                State.Player.WorkedHoursToday = 0m;

            var operationPaid = Math.Min(company.Cash, operations);
            company.Cash -= operationPaid;
            State.Treasury += operationPaid;
            company.LastCosts += operationPaid;

            if (company.Debt > 0m && company.Cash > 80_000m)
            {
                var repayment = Math.Min(company.Debt, Math.Min(company.Cash - 60_000m, company.Debt * 0.02m));
                if (repayment > 0m)
                {
                    company.Cash -= repayment;
                    company.Debt -= repayment;
                    State.Treasury += repayment;
                    company.LastCosts += repayment;
                }
            }
        }
    }

    private void ProcessConsumption(DeterministicRng rng)
    {
        foreach (var citizen in State.Citizens.Where(c => c.Alive && c.AgeYears >= 12))
        {
            if (citizen.Cash <= 0m) continue;
            var market = State.Markets[rng.NextInt(0, State.Markets.Count)];
            if (market.Stock <= 0.01m) continue;

            var budget = citizen.EmployedCompanyId is null
                ? 5m + citizen.SkillTier
                : 12m + citizen.SkillTier * 2m;
            budget = Math.Min(budget, citizen.Cash);
            if (budget <= 0m) continue;

            var sellers = State.Companies.Where(c => c.Open && c.ProductFamily == market.Family).ToArray();
            if (sellers.Length == 0) continue;

            var seller = SelectCompetitiveSeller(sellers, citizen, market, rng);
            var unitPrice = decimal.Round(market.Price * Clamp(seller.PriceMultiplier, 0.72m, 1.45m), 2);
            var units = Math.Min(market.Stock, budget / Math.Max(1m, unitPrice));
            var spend = Math.Min(citizen.Cash, decimal.Round(units * unitPrice, 2));
            if (spend <= 0m) continue;

            citizen.Cash -= spend;
            seller.Cash += spend;
            seller.LastRevenue += spend;
            market.Stock -= units;
            market.DailyDemand += units;
            citizen.Hunger = Clamp(citizen.Hunger - (market.Family == "Alimentos" ? 18m : 2m), 0m, 100m);
            citizen.Happiness = Clamp(citizen.Happiness + seller.ProductQuality * 0.25m, 0m, 100m);
        }

        if (State.CurrentDay % 7 == 0)
        {
            foreach (var citizen in State.Citizens.Where(c => c.Alive && c.AgeYears >= 18))
            {
                var district = State.Districts.First(d => d.Id == citizen.DistrictId);
                var rent = decimal.Round(6m * district.RentIndex, 2);
                var paid = Math.Min(citizen.Cash, rent);
                citizen.Cash -= paid;
                State.Treasury += paid;
            }
        }
    }

    private void ProcessProductionAndPrices()
    {
        foreach (var company in State.Companies.Where(c => c.Open).ToArray())
        {
            var market = State.Markets.First(m => m.Family == company.ProductFamily);
            var workforce = company.EmployeeIds.Count + (company.PlayerOwned ? 1 : 0);
            var humanFactor = 0.70m + Clamp(company.EmployeeMorale, 0m, 1m) * 0.30m;
            var qualityFactor = 0.86m + Clamp(company.ProductQuality, 0m, 1m) * 0.18m;
            var innovationFactor = 0.92m + Clamp(company.Innovation, 0m, 1m) * 0.16m;
            var output = decimal.Round((2m + workforce * 2.2m) * company.Productivity * humanFactor * qualityFactor * innovationFactor, 3);
            market.Stock += output;
            market.DailySupply += output;

            var margin = company.LastRevenue - company.LastCosts;
            if (margin < 0m) company.LossDays++;
            else company.LossDays = Math.Max(0, company.LossDays - 2);

            if (margin > 0m)
            {
                var tax = decimal.Round(margin * 0.03m, 2);
                tax = Math.Min(tax, company.Cash);
                company.Cash -= tax;
                State.Treasury += tax;
                company.LastCosts += tax;
                company.LastTaxes = tax;
            }

            if (company.LossDays > 18 && company.EmployeeIds.Count > 1)
                company.DesiredEmployees = Math.Max(1, company.DesiredEmployees - 1);

            var closureThreshold = company.PlayerOwned ? 95 : 70;
            if (company.LossDays > closureThreshold && company.Debt >= CreditLimit(company) * 0.90m && company.Cash < company.BaseWage * 3m)
                CloseCompany(company, "receita insuficiente → perdas persistentes → dívida elevada → crédito esgotado → insolvência");
        }

        foreach (var market in State.Markets)
        {
            var effectiveSupply = Math.Max(1m, market.DailySupply + (market.Stock * 0.03m));
            var pressure = (market.DailyDemand - effectiveSupply) / effectiveSupply;
            var adjustment = Clamp(pressure * 0.035m, -0.04m, 0.04m);
            var next = market.Price * (1m + adjustment);
            market.Price = decimal.Round(Clamp(next, market.BasePrice * 0.45m, market.BasePrice * 3m), 2);
            market.Stock = Math.Max(0m, market.Stock);
        }
    }

    private void ProcessEmployment(DeterministicRng rng)
    {
        foreach (var company in State.Companies.Where(c => c.Open && c.EmployeeIds.Count < c.DesiredEmployees))
        {
            if (company.Cash < company.BaseWage * 20m) continue;
            if (!rng.Chance(0.18)) continue;

            var candidate = State.Citizens
                .Where(c => c.Alive && c.AgeYears is >= 18 and < 66 && c.EmployedCompanyId is null)
                .OrderByDescending(c => c.SkillTier)
                .FirstOrDefault();
            if (candidate is null) continue;

            candidate.EmployedCompanyId = company.Id;
            candidate.DailyWage = decimal.Round(company.BaseWage * (0.72m + candidate.SkillTier * 0.08m), 2);
            company.EmployeeIds.Add(candidate.Id);
            AddHistory("Contratação", $"{company.Name} contratou {candidate.Name}.",
                "vaga aberta + caixa suficiente + trabalhador disponível", company.Id);
        }
    }

    private void ProcessDemography(DeterministicRng rng)
    {
        foreach (var citizen in State.Citizens.Where(c => c.Alive && c.AgeYears >= 76).ToArray())
        {
            var chance = 0.00012 + Math.Max(0, citizen.AgeYears - 80) * 0.00003;
            if (!rng.Chance(chance)) continue;

            citizen.Alive = false;
            if (citizen.EmployedCompanyId is int companyId)
            {
                var company = State.Companies.FirstOrDefault(c => c.Id == companyId);
                company?.EmployeeIds.Remove(citizen.Id);
            }
            var heir = State.Citizens.FirstOrDefault(c => c.Alive && c.HouseholdId == citizen.HouseholdId && c.Id != citizen.Id);
            if (heir is not null) heir.Cash += citizen.Cash;
            else State.Treasury += citizen.Cash;
            citizen.Cash = 0m;
            citizen.EmployedCompanyId = null;
            AddHistory("Falecimento", $"{citizen.Name} faleceu aos {citizen.AgeYears} anos.", "envelhecimento populacional", citizen.Id);
        }

        if (State.CurrentDay % 30 != 0) return;

        var mothers = State.Citizens.Where(c => c.Alive && c.AgeYears is >= 22 and <= 40).ToArray();
        var births = Math.Min(5, Math.Max(1, State.Population / 600));
        for (var i = 0; i < births && mothers.Length > 0; i++)
        {
            var parent = mothers[rng.NextInt(0, mothers.Length)];
            var baby = new CitizenState
            {
                Id = State.NextCitizenId++,
                Name = $"Cidadão {State.NextCitizenId:00000}",
                AgeDays = 0,
                Cash = 0m,
                DistrictId = parent.DistrictId,
                HouseholdId = parent.HouseholdId,
                SkillTier = 1,
                Hunger = 10m
            };
            State.Citizens.Add(baby);
            AddHistory("Nascimento", "Nasceu um novo cidadão.", "dinâmica demográfica", baby.Id);
        }
    }

    private void ProcessCompanyBirths(DeterministicRng rng)
    {
        if (State.CurrentDay % 30 != 0) return;
        var opportunity = State.Markets
            .OrderByDescending(m => m.DailyDemand / Math.Max(1m, m.Stock))
            .First();
        var sector = ContentCatalog.Sectors.First(s => s.ProductFamily == opportunity.Family);
        var founder = State.Citizens
            .Where(c => c.Alive && c.AgeYears is >= 24 and < 66 && c.Cash >= 8_000m)
            .OrderByDescending(c => c.Cash)
            .FirstOrDefault();
        if (founder is null || !rng.Chance(0.65)) return;

        const decimal capital = 6_000m;
        founder.Cash -= capital;
        var company = new CompanyState
        {
            Id = State.NextCompanyId++,
            Name = $"{founder.Name.Split(' ')[0]} {sector.Name} {State.NextCompanyId:000}",
            Sector = sector.Name,
            ProductFamily = sector.ProductFamily,
            Archetype = "Empresa emergente",
            DistrictId = founder.DistrictId,
            Cash = capital,
            BaseWage = 105m,
            Reputation = 0.35m,
            Productivity = sector.Productivity,
            DesiredEmployees = 3
        };
        State.Companies.Add(company);
        AddHistory("Nova empresa", $"{company.Name} entrou no mercado de {sector.ProductFamily}.",
            "demanda relativa alta + cidadão capitalizado → empreendimento", company.Id);
    }

    private void ProcessPlayerSuccession(DeterministicRng rng)
    {
        if (State.Player.AgeYears < 82) return;
        var chance = 0.00018 + Math.Max(0, State.Player.AgeYears - 85) * 0.00006;
        if (!rng.Chance(chance)) return;

        var oldName = State.Player.Name;
        var inheritance = decimal.Round(State.Player.Cash * 0.70m, 2);
        State.Treasury += State.Player.Cash - inheritance;
        State.Player.Generation++;
        State.Player.AgeDays = 18 * 365;
        State.Player.Cash = inheritance;
        State.Player.EmployerCompanyId = null;
        State.Player.DailyWage = 0m;
        State.Player.Hunger = 15m;
        State.Player.Energy = 100m;
        State.Player.Children = Math.Max(0, State.Player.Children - 1);
        State.Player.Name = $"{oldName} — G{State.Player.Generation}";
        AddHistory("Sucessão", $"A partida continuou com a geração {State.Player.Generation}.",
            "falecimento do personagem → sucessão patrimonial");
    }

    private void TryBorrow(CompanyState company, decimal requested)
    {
        if (requested <= 0m) return;
        var limit = CreditLimit(company);
        var available = Math.Max(0m, limit - company.Debt);
        var amount = Math.Min(requested, Math.Min(available, State.Treasury));
        if (amount <= 0m) return;
        State.Treasury -= amount;
        company.Cash += amount;
        company.Debt += amount;
    }

    private decimal CreditLimit(CompanyState company) =>
        40_000m + (220_000m * Clamp(company.Reputation, 0m, 1m));

    private void CloseCompany(CompanyState company, string cause)
    {
        company.Open = false;
        company.OperatingStatus = "Encerrada";
        company.ClosureCause = cause;
        foreach (var workerId in company.EmployeeIds.ToArray())
        {
            var worker = State.Citizens.FirstOrDefault(c => c.Id == workerId);
            if (worker is not null)
            {
                worker.EmployedCompanyId = null;
                worker.DailyWage = 0m;
            }
        }
        company.EmployeeIds.Clear();
        if (State.Player.EmployerCompanyId == company.Id)
        {
            State.Player.EmployerCompanyId = null;
            State.Player.DailyWage = 0m;
        }
        if (State.Player.BusinessCompanyId == company.Id)
            State.Player.BusinessCompanyId = null;
        AddHistory("Falência", $"{company.Name} encerrou as atividades.", cause, company.Id);
    }

    private DistrictState CompanyDistrict(CompanyState company) =>
        State.Districts.First(d => d.Id == company.DistrictId);

    private void AddHistory(string kind, string summary, string cause, int? entityId = null) =>
        State.History.Add(new HistoryEvent { Day = State.CurrentDay, Kind = kind, Summary = summary, Cause = cause, EntityId = entityId });

    private void CompactHistory()
    {
        const int maxEvents = 2_500;
        if (State.History.Count <= maxEvents) return;
        State.History = State.History
            .Where(e => e.Kind is "Falência" or "Nova empresa" or "Sucessão" or "Mundo" || e.Day > State.CurrentDay - 365)
            .TakeLast(maxEvents)
            .ToList();
    }

    private static long DailySeed(long seed, int day) =>
        unchecked(seed ^ ((long)day * -7046029254386353131L));

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        value < min ? min : value > max ? max : value;
}

public static class SimulationValidator
{
    public static void Validate(GameState state)
    {
        if (state.SchemaVersion != GameState.CurrentSchemaVersion)
            throw new InvalidDataException($"Schema de save incompatível: {state.SchemaVersion}.");
        if (state.Districts.Count == 0) throw new InvalidDataException("Cidade sem distritos.");
        if (state.Markets.Count == 0) throw new InvalidDataException("Cidade sem mercados.");
        if (state.Markets.Any(m => m.Stock < 0m || m.Price <= 0m))
            throw new InvalidDataException("Mercado com estoque ou preço inválido.");
        if (state.Citizens.Any(c => c.Cash < 0m))
            throw new InvalidDataException("Cidadão com caixa negativo.");
        if (state.Player.Cash < 0m)
            throw new InvalidDataException("Jogador com caixa negativo.");
        if (state.CurrentHour is < 0 or > 23)
            throw new InvalidDataException("Hora da simulação inválida.");
        if (state.Player.Health is < 0m or > 100m || state.Player.Stress is < 0m or > 100m ||
            state.Player.Happiness is < 0m or > 100m || state.Player.Social is < 0m or > 100m)
            throw new InvalidDataException("Indicadores de vida do jogador fora do intervalo.");
        if (state.Companies.Any(c => c.BrandAwareness is < 0m or > 1m || c.ProductQuality is < 0m or > 1m ||
                                     c.EmployeeMorale is < 0m or > 1m || c.PriceMultiplier <= 0m))
            throw new InvalidDataException("Indicadores empresariais inválidos.");

        var openCompanyIds = state.Companies.Where(c => c.Open).Select(c => c.Id).ToHashSet();
        foreach (var citizen in state.Citizens.Where(c => c.Alive && c.EmployedCompanyId is not null))
        {
            if (!openCompanyIds.Contains(citizen.EmployedCompanyId!.Value))
                throw new InvalidDataException("Vínculo empregatício aponta para empresa fechada ou inexistente.");
        }
    }
}
