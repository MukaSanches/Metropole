namespace Metropole.Sim;

public static class AffordanceCatalog
{
    private static readonly InteractionAffordanceDefinition[] Definitions =
    [
        new("bed.sleep", "Bed", "Dormir", 8, "Fadiga", 85m, 0m, "sleep"),
        new("bed.nap", "Bed", "Cochilar", 2, "Fadiga", 28m, 0m, "sleep"),
        new("bed.relax", "Bed", "Relaxar", 1, "Conforto", 18m, 0m, "sit"),
        new("chair.sit", "Chair", "Sentar", 1, "Conforto", 12m, 0m, "sit"),
        new("fridge.eat", "Fridge", "Comer", 1, "Fome", 48m, 12m, "eat"),
        new("fridge.cook", "Fridge", "Preparar comida", 1, "Fome", 60m, 22m, "cook"),
        new("shower.use", "Shower", "Tomar banho", 1, "Higiene", 82m, 4m, "shower"),
        new("toilet.use", "Toilet", "Usar banheiro", 1, "Banheiro", 92m, 0m, "toilet"),
        new("tv.watch", "Television", "Assistir TV", 2, "Diversão", 36m, 0m, "sit"),
        new("computer.study", "Computer", "Estudar", 2, "Curiosidade", 28m, 8m, "computer"),
        new("computer.work", "Computer", "Trabalhar", 4, "Carreira", 22m, 0m, "computer"),
        new("computer.job", "Computer", "Procurar emprego", 2, "Carreira", 34m, 0m, "computer"),
        new("phone.message", "Phone", "Enviar mensagem", 1, "Social", 18m, 0m, "phone"),
        new("phone.call", "Phone", "Ligar", 1, "Social", 26m, 0m, "phone"),
        new("car.drive", "Vehicle", "Dirigir", 1, "Mobilidade", 35m, 8m, "drive")
    ];

    public static IReadOnlyList<InteractionAffordanceDefinition> All => Definitions;

    public static IReadOnlyList<InteractionAffordanceDefinition> ForProvider(string provider) =>
        Definitions.Where(x => x.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static IReadOnlyList<InteractionAffordanceDefinition> ForNeed(string need) =>
        Definitions.Where(x => x.SatisfiesNeed.Equals(need, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.NeedRelief).ToArray();

    public static InteractionAffordanceDefinition? BestForNeed(string need, decimal availableCash)
    {
        InteractionAffordanceDefinition? best = null;
        decimal bestScore = decimal.MinValue;

        foreach (var item in Definitions)
        {
            if (!item.SatisfiesNeed.Equals(need, StringComparison.OrdinalIgnoreCase) || item.Cost > availableCash)
                continue;

            var score = item.NeedRelief / Math.Max(1, item.DurationHours);
            if (score < bestScore) continue;
            if (score == bestScore && best is not null && item.Cost >= best.Cost) continue;
            best = item;
            bestScore = score;
        }

        return best;
    }
}

public static class AaaScaleProbe
{
    public static AaaScaleProbeResult ProbeLodAllocation(int population, int activeCap = 96)
    {
        if (population < 0) throw new ArgumentOutOfRangeException(nameof(population));
        if (activeCap < 1) throw new ArgumentOutOfRangeException(nameof(activeCap));

        var interactive = population > 0 ? 1 : 0;
        var active = Math.Min(Math.Max(0, population - interactive), activeCap);
        var regional = Math.Min(Math.Max(0, population - interactive - active), activeCap * 4);
        var statistical = Math.Max(0, population - interactive - active - regional);
        return new(population, statistical, regional, active, interactive);
    }
}
