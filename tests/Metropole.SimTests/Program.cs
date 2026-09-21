using System.Diagnostics;
using Metropole.Sim;

var tests = new List<(string Name, Action Run)>
{
    ("catálogo mínimo e funcional", TestCatalog),
    ("geração determinística", TestGenerationDeterminism),
    ("simulação determinística", TestSimulationDeterminism),
    ("conservação monetária básica", TestMoneyConservation),
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
    Console.WriteLine($"       5 anos simulados em {sw.Elapsed.TotalSeconds:N2}s; população {engine.State.Population:N0}; empresas {engine.State.OpenCompanies:N0}.");
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
