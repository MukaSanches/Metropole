using System.Diagnostics;
using Metropole.Sim;

var tests = new List<(string Name, Action Run)>
{
    ("catálogo mínimo e funcional", TestCatalog),
    ("geração determinística", TestGenerationDeterminism),
    ("simulação determinística", TestSimulationDeterminism),
    ("conservação monetária básica", TestMoneyConservation),
    ("ações jogáveis preservam invariantes", TestPlayableActions),
    ("relógio horário e vida do jogador", TestHourlyLife),
    ("branding, estratégia e finanças empresariais", TestDeepBusiness),
    ("amizade, namoro, casamento e família", TestSocialLifecycle),
    ("matriz multi-seed de estabilidade econômica", TestEconomyStressMatrix),
    ("save/load atômico", TestSaveRoundTrip),
    ("simulação longa sem invariantes quebradas", TestLongRun)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"[PASS] {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"[FAIL] {test.Name}: {ex}");
    }
}

Console.WriteLine($"{tests.Count - failures}/{tests.Count} testes aprovados.");
return failures == 0 ? 0 : 1;

static void TestCatalog()
{
    var m = ContentCatalog.Metrics;
    Check(m.ProfessionArchetypes >= 300, "menos de 300 arquétipos profissionais");
    Check(m.CareerCombinations >= 1_000, "menos de 1.000 combinações de carreira");
    Check(m.BusinessArchetypes >= 500, "menos de 500 arquétipos empresariais");
    Check(m.Products >= 2_000, "menos de 2.000 produtos");
    Check(m.Resources >= 500, "menos de 500 recursos");
    Check(m.Buildings >= 300, "menos de 300 edifícios");
    Check(m.Skills >= 200, "menos de 200 competências");
    Check(m.Events >= 1_000, "menos de 1.000 eventos");
    Check(m.Technologies >= 300, "menos de 300 tecnologias");
}

static void TestGenerationDeterminism()
{
    var a = WorldGenerator.Generate(424242, "Teste");
    var b = WorldGenerator.Generate(424242, "Teste");
    Check(a.Population == b.Population, "população inicial divergiu");
    Check(a.Companies[0].Name == b.Companies[0].Name, "empresa inicial divergiu");
    Check(a.Companies[0].Cash == b.Companies[0].Cash, "caixa inicial divergiu");
    Check(a.Districts[4].RentIndex == b.Districts[4].RentIndex, "bairro divergiu");
    Check(a.Citizens[100].Name == b.Citizens[100].Name, "cidadão divergiu");
}

static void TestSimulationDeterminism()
{
    var a = new SimulationEngine(WorldGenerator.Generate(9999, "A"));
    var b = new SimulationEngine(WorldGenerator.Generate(9999, "A"));
    a.AdvanceDays(365);
    b.AdvanceDays(365);
    Check(a.State.Population == b.State.Population, "população divergiu após 1 ano");
    Check(a.State.OpenCompanies == b.State.OpenCompanies, "empresas divergiram após 1 ano");
    Check(a.State.Treasury == b.State.Treasury, "tesouro divergiu");
    for (var i = 0; i < a.State.Markets.Count; i++)
        Check(a.State.Markets[i].Price == b.State.Markets[i].Price, $"preço divergiu em {a.State.Markets[i].Family}");
}

static void TestMoneyConservation()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(73, "Caixa"));
    var before = engine.State.TotalLiquidMoney();
    engine.AdvanceOneDay();
    var after = engine.State.TotalLiquidMoney();
    var delta = Math.Abs(before - after);
    Check(delta <= 0.02m, $"dinheiro líquido variou {delta:N4} no tick básico");
}

static void TestPlayableActions()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(2026, "Jogador"));

    var job = engine.GetJobBoard().First();
    Check(engine.AcceptJob(job.Id), "não aceitou emprego válido");
    Check(engine.LeaveJob(), "não saiu do emprego");
    Check(engine.State.Player.EmployerCompanyId is null, "emprego não foi limpo");

    var moneyBeforeFood = engine.State.TotalLiquidMoney();
    var bought = engine.BuyFood(3);
    Check(bought > 0, "não comprou alimentação");
    Check(Math.Abs(engine.State.TotalLiquidMoney() - moneyBeforeFood) <= 0.02m, "compra de alimento criou/destruiu dinheiro");

    var target = engine.State.Districts.First(d => d.Id != engine.State.Player.DistrictId);
    var moneyBeforeMove = engine.State.TotalLiquidMoney();
    Check(engine.MovePlayerDistrict(target.Id), "mudança válida falhou");
    Check(engine.State.Player.DistrictId == target.Id, "bairro do jogador não mudou");
    Check(Math.Abs(engine.State.TotalLiquidMoney() - moneyBeforeMove) <= 0.02m, "mudança criou/destruiu dinheiro");

    engine.State.Player.Cash = Math.Max(engine.State.Player.Cash, 10_000m);
    Check(engine.OpenPlayerBusiness(ContentCatalog.Sectors[0].Name), "abertura de empresa falhou");
    var moneyBeforeInvestment = engine.State.TotalLiquidMoney();
    Check(engine.InvestInPlayerBusiness(500m), "aporte falhou");
    Check(Math.Abs(engine.State.TotalLiquidMoney() - moneyBeforeInvestment) <= 0.02m, "aporte criou/destruiu dinheiro");
    Check(engine.AdjustPlayerBusinessHeadcount(1), "ajuste de quadro falhou");

    SimulationValidator.Validate(engine.State);
}


static void TestHourlyLife()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(707, "Vida"));
    var startDay = engine.State.CurrentDay;
    var startHour = engine.State.CurrentHour;

    engine.AdvanceHours(5);
    Check(engine.State.CurrentHour == (startHour + 5) % 24, "relógio horário não avançou");
    Check(engine.State.Player.CurrentActivity.Length > 0, "atividade do jogador não foi definida");

    var remaining = 24 - engine.State.CurrentHour;
    engine.AdvanceHours(remaining);
    Check(engine.State.CurrentDay == startDay + 1, "virada de dia não ocorreu no relógio horário");
    Check(engine.State.Player.Health is >= 0m and <= 100m, "saúde fora do intervalo");
    Check(engine.State.Player.Stress is >= 0m and <= 100m, "estresse fora do intervalo");
    Check(engine.State.Citizens.All(c => c.Energy is >= 0m and <= 100m), "energia de cidadão fora do intervalo");
}

static void TestDeepBusiness()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(808, "Marca"));
    engine.State.Player.Cash = 25_000m;

    Check(engine.OpenPlayerBusiness(ContentCatalog.Sectors[0].Name), "empresa do jogador não abriu");
    var company = engine.State.Companies.First(c => c.Id == engine.State.Player.BusinessCompanyId);

    Check(engine.ConfigureBrand("Aurora", "Feito para durar."), "branding não foi aplicado");
    Check(company.BrandName == "Aurora", "nome de marca não persistiu");
    Check(engine.SetPricingStrategy("Premium"), "estratégia de preço falhou");
    Check(company.PriceMultiplier > 1m, "preço premium não foi aplicado");
    Check(engine.AdjustMarketingBudget(30m), "orçamento de marketing não foi alterado");

    company.Cash += 10_000m;
    Check(engine.InvestInQuality(1_500m), "investimento em qualidade falhou");
    Check(engine.InvestInInnovation(1_500m), "investimento em inovação falhou");

    engine.AdvanceDays(35);
    Check(company.FinanceHistory.Count > 0, "histórico financeiro não foi gerado");
    Check(company.BrandAwareness is >= 0m and <= 1m, "awareness inválido");
    Check(company.ProductQuality is >= 0m and <= 1m, "qualidade inválida");
    Check(company.MarketShare is >= 0m and <= 1m, "market share inválido");
    SimulationValidator.Validate(engine.State);
}


static void TestSocialLifecycle()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(1515, "Social"));
    var state = engine.State;
    state.CurrentHour = 12;
    state.Player.Cash = 25_000m;

    var person = state.Citizens
        .First(c => c.Alive && c.AgeYears >= 20 && c.DistrictId == state.Player.DistrictId);

    person.CurrentActivity = "Lazer";
    person.PartnerCitizenId = null;
    person.IsPlayerPartner = false;
    person.PlayerFamiliarity = 0m;
    person.PlayerAffinity = 0m;
    person.PlayerTrust = 0m;

    Check(engine.MeetPerson(person.Id), "não foi possível conhecer pessoa acessível");
    Check(person.PlayerFamiliarity > 0m, "familiaridade não foi criada");

    var moneyBeforeHangout = state.TotalLiquidMoney();
    Check(engine.HangOutWithPerson(person.Id), "saída social válida falhou");
    Check(Math.Abs(state.TotalLiquidMoney() - moneyBeforeHangout) <= 0.02m, "saída social criou/destruiu dinheiro");

    person.PlayerFamiliarity = 80m;
    person.PlayerAffinity = 82m;
    person.PlayerTrust = 72m;
    person.CurrentActivity = "Lazer";
    Check(engine.AskToDate(person.Id), "pedido de namoro válido falhou");
    Check(state.Player.PartnerCitizenId == person.Id && person.IsPlayerPartner, "namoro não vinculou jogador e cidadão");

    state.Player.RelationshipStartDay = state.CurrentDay - 35;
    person.PlayerAffinity = 90m;
    person.PlayerTrust = 84m;
    state.Player.Cash = Math.Max(state.Player.Cash, 20_000m);
    var moneyBeforeMarriage = state.TotalLiquidMoney();
    Check(engine.ProposeMarriage(), "casamento válido falhou");
    Check(state.Player.RelationshipStatus == "Casado", "estado de casamento não foi aplicado");
    Check(Math.Abs(state.TotalLiquidMoney() - moneyBeforeMarriage) <= 0.02m, "casamento criou/destruiu dinheiro");

    state.Player.MarriageDay = state.CurrentDay - 40;
    person.PlayerAffinity = 92m;
    person.PlayerTrust = 90m;
    state.Player.Cash = Math.Max(state.Player.Cash, 20_000m);
    var populationBefore = state.Population;
    var moneyBeforeChild = state.TotalLiquidMoney();
    Check(engine.PlanChild(), "planejamento familiar válido falhou");
    Check(state.Player.Children >= 1, "filho não foi registrado");
    Check(state.Population == populationBefore + 1, "novo filho não entrou na população");
    Check(Math.Abs(state.TotalLiquidMoney() - moneyBeforeChild) <= 0.02m, "planejamento familiar criou/destruiu dinheiro");

    SimulationValidator.Validate(state);
}

static void TestEconomyStressMatrix()
{
    var seeds = new long[] { 11, 73, 707, 2026, 8080, 424242, 20260921, 998877 };
    var unemployment = new List<decimal>();
    var companies = new List<int>();

    foreach (var seed in seeds)
    {
        var engine = new SimulationEngine(WorldGenerator.Generate(seed, $"Stress {seed}"));
        engine.AdvanceDays(1_825);
        SimulationValidator.Validate(engine.State);

        unemployment.Add(engine.State.UnemploymentRate);
        companies.Add(engine.State.OpenCompanies);

        Check(engine.State.Population > 900, $"seed {seed}: população caiu demais");
        Check(engine.State.OpenCompanies >= 60, $"seed {seed}: poucas empresas abertas ({engine.State.OpenCompanies})");
        Check(engine.State.UnemploymentRate < 0.68m, $"seed {seed}: desemprego extremo ({engine.State.UnemploymentRate:P1})");
        Check(engine.State.Companies.Where(c => c.Open).All(c => c.Cash >= 0m), $"seed {seed}: empresa aberta com caixa negativo");
        Check(engine.State.Companies.Where(c => c.Open).All(c => c.MarketShare is >= 0m and <= 1m), $"seed {seed}: market share inválido");
    }

    var avgUnemployment = unemployment.Average();
    var avgCompanies = companies.Average();
    Check(avgUnemployment < 0.55m, $"média de desemprego alta: {avgUnemployment:P1}");
    Check(avgCompanies >= 90, $"média de empresas baixa: {avgCompanies:N1}");
    Console.WriteLine($"       matriz {seeds.Length} seeds/5 anos: desemprego médio {avgUnemployment:P1}; empresas médias {avgCompanies:N1}; pior desemprego {unemployment.Max():P1}; mínimo empresas {companies.Min()}.");
}

static void TestSaveRoundTrip()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(123456, "Save"));
    engine.AdvanceDays(45);
    var dir = Path.Combine(Path.GetTempPath(), "metropole-tests", Guid.NewGuid().ToString("N"));
    var file = Path.Combine(dir, "save.json");
    SaveStore.Save(file, engine.State);
    var loaded = SaveStore.Load(file);
    Check(loaded.CurrentDay == 45, "dia não preservado");
    Check(loaded.Seed == 123456, "seed não preservada");
    Check(loaded.Population == engine.State.Population, "população não preservada");
    Check(loaded.Player.Cash == engine.State.Player.Cash, "caixa do jogador não preservado");
    Directory.Delete(dir, true);
}

static void TestLongRun()
{
    var engine = new SimulationEngine(WorldGenerator.Generate(20260921, "LongRun"));
    var sw = Stopwatch.StartNew();
    engine.AdvanceDays(1_825);
    sw.Stop();
    SimulationValidator.Validate(engine.State);
    Check(engine.State.Population > 0, "população zerou");
    Check(engine.State.Markets.All(m => m.Stock >= 0m && m.Price > 0m), "mercado inválido");
    Check(engine.State.Companies.Any(c => c.Open), "todas as empresas fecharam");
    Check(engine.State.OpenCompanies >= 60, $"ecossistema empresarial colapsou: {engine.State.OpenCompanies} empresas abertas");
    Check(engine.State.UnemploymentRate < 0.55m, $"desemprego estrutural excessivo: {engine.State.UnemploymentRate:P1}");
    Check(engine.State.Companies.Where(c => c.Open).All(c => c.BrandAwareness is >= 0m and <= 1m), "marca fora do intervalo");
    Console.WriteLine($"       5 anos simulados em {sw.Elapsed.TotalSeconds:N2}s; população {engine.State.Population:N0}; empresas {engine.State.OpenCompanies:N0}; desemprego {engine.State.UnemploymentRate:P1}.");
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
