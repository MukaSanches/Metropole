namespace Metropole.Sim;

public sealed class EverydayLifeState
{
    public decimal Hydration { get; set; } = 80;
    public decimal Hygiene { get; set; } = 80;
    public decimal HomeCare { get; set; } = 75;
    public decimal Fun { get; set; } = 65;
    public decimal Purpose { get; set; } = 55;
    public long LastHour { get; set; } = -1;
    public string Aspiration { get; set; } = "Equilíbrio";
    public Dictionary<string, int> PracticeHours { get; set; } = [];
    public Dictionary<string, long> LastCompleted { get; set; } = [];
    public List<LifeMemory> Journal { get; set; } = [];
    public List<string> Milestones { get; set; } = [];
    public string? PendingEvent { get; set; }
    public int LastEventWeek { get; set; } = -1;
}

public sealed record LifeMemory(int Day, int Hour, string Summary);
public sealed record EverydayAction(string Id, string Name, string Category, int Hours, decimal Cost,
    string Activity, string Skill, decimal Energy, decimal Hunger, decimal Health, decimal Stress,
    decimal Social, decimal Fun, decimal Hygiene, decimal Hydration, decimal Home, string Description);
public sealed record LifeResult(bool Success, string Message);

public static class EverydayLife
{
    public static readonly IReadOnlyList<EverydayAction> Actions = Array.AsReadOnly(new EverydayAction[]
    {
        new("water", "Beber água e fazer uma pausa", "Autocuidado", 1, 0, "Em casa", "", 2, 0, 0, -2, 0, 0, 0, 70, 0, "Recupera hidratação; reduz um pouco o estresse."),
        new("shower", "Banho e cuidados pessoais", "Autocuidado", 1, 4, "Em casa", "", 0, 0, 0, -4, 0, 0, 65, 0, 0, "Recupera higiene. O custo vai para os serviços da cidade."),
        new("cook", "Cozinhar uma refeição", "Casa", 2, 22, "Cozinhando", "Culinária", -2, -48, 1, -3, 0, 4, -3, 5, -2, "Saciedade e prática culinária; ingredientes custam Cr$ 22."),
        new("clean", "Limpar e organizar a casa", "Casa", 2, 8, "Organizando a casa", "Organização", -6, 2, 0, -5, 0, -2, -4, 0, 45, "Melhora a casa, que perde conservação com o tempo."),
        new("repair", "Pequenos reparos domésticos", "Casa", 3, 35, "Reparando a casa", "Manutenção", -8, 3, 0, -3, 0, 3, -6, 0, 55, "Pratica manutenção e recupera a conservação da casa."),
        new("read", "Ler um livro", "Hobbies", 2, 0, "Lendo", "Leitura", -2, 0, 0, -6, 0, 18, 0, 0, 0, "Lazer tranquilo e prática de leitura sem custo."),
        new("music", "Praticar música", "Hobbies", 2, 5, "Praticando música", "Música", -3, 0, 0, -8, 0, 24, 0, 0, 0, "Prática musical, diversão e alívio do estresse."),
        new("paint", "Pintar e criar", "Hobbies", 2, 15, "Pintando", "Artes", -3, 0, 0, -7, 0, 26, -2, 0, 0, "Materiais, expressão artística e diversão."),
        new("garden", "Cuidar de plantas", "Casa", 2, 10, "Cuidando das plantas", "Jardinagem", -4, 0, 1, -6, 0, 16, -5, -3, 8, "Melhora o ambiente da casa e desenvolve jardinagem."),
        new("walk", "Passear no parque", "Lazer", 2, 0, "Lazer", "Condicionamento", -5, 3, 2, -9, 2, 20, -2, -8, 0, "Passeio gratuito; fica indisponível durante chuva."),
        new("cinema", "Ir ao cinema", "Lazer", 3, 35, "Lazer", "", -2, 2, 0, -12, 3, 40, 0, 0, 0, "Três horas de lazer, com ingresso e gasto de tempo."),
        new("meditate", "Meditar e desacelerar", "Autocuidado", 1, 0, "Meditando", "Autocontrole", 3, 0, 0, -13, 0, 5, 0, 0, 0, "Uma pausa curta para reduzir tensão."),
        new("doctor", "Consulta de saúde", "Autocuidado", 3, 90, "Cuidando da saúde", "", -2, 0, 18, -5, 0, 0, 0, 10, 0, "Recupera saúde; precisa de dinheiro e horário de atendimento."),
        new("volunteer", "Ajudar a comunidade", "Comunidade", 3, 0, "Socializando", "Comunicação", -8, 4, 0, -6, 15, 14, -3, -5, 0, "Tempo doado melhora vida social e senso de propósito."),
        new("family", "Preparar um encontro em família", "Comunidade", 3, 40, "Socializando", "Convivência", -3, -15, 0, -10, 22, 20, 0, 0, -5, "Requer parceiro vivo no bairro; melhora afinidade e confiança."),
        new("course", "Praticar um projeto de programação", "Projetos", 3, 12, "Estudando", "Programação", -5, 2, 0, 3, -2, 12, 0, -3, 0, "Acumula prática persistente; cada 8h aumenta a competência."),
        new("budget", "Organizar o orçamento pessoal", "Projetos", 1, 0, "Planejando", "Finanças", -1, 0, 0, -5, 0, 2, 0, 0, 0, "Pratica finanças sem criar dinheiro artificialmente."),
        new("rest", "Descansar em casa", "Autocuidado", 2, 0, "Descansando", "", 16, 0, 1, -8, 0, 8, 0, 0, 0, "Recupera energia sem compromisso ou custo.")
    });

    public static long Clock(GameState s) => (long)s.CurrentDay * 24 + s.CurrentHour;
    private static decimal Meter(decimal value) => Math.Clamp(value, 0, 100);

    public static void Initialize(GameState s)
    {
        s.Life ??= new EverydayLifeState();
        if (s.Life.LastHour < 0) s.Life.LastHour = Clock(s);
    }

    public static string Unavailable(GameState s, EverydayAction a)
    {
        if (s.Player.Cash < a.Cost) return $"Faltam Cr$ {a.Cost - s.Player.Cash:N2}.";
        if (a.Energy < 0 && s.Player.Energy < 12) return "Descanse antes: energia abaixo de 12.";
        if (s.Life.LastCompleted.TryGetValue(a.Id, out var last) && Clock(s) - last < 4)
            return "Faça outra atividade: esta precisa de 4h de intervalo.";
        if (a.Id == "walk" && s.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase)) return "Está chovendo: escolha lazer em casa.";
        if (a.Id == "doctor" && (s.CurrentHour < 8 || s.CurrentHour + a.Hours > 18)) return "Atendimento entre 08h e 18h; reserve 3h.";
        if (a.Id == "family" && !s.Citizens.Any(c => c.Alive && c.Id == s.Player.PartnerCitizenId && c.DistrictId == s.Player.DistrictId))
            return "É preciso ter um parceiro vivo no mesmo bairro.";
        return "";
    }

    public static LifeResult PerformEverydayAction(this SimulationEngine engine, string id)
    {
        var s = engine.State;
        var a = Actions.FirstOrDefault(x => x.Id == id);
        if (a is null) return new(false, "Atividade desconhecida.");
        var reason = Unavailable(s, a);
        if (reason.Length > 0) return new(false, reason);
        var generation = s.Player.Generation;
        // Services/ingredients are paid to the city; money is transferred, never created.
        s.Player.Cash -= a.Cost;
        s.Treasury += a.Cost;
        engine.AdvanceActivity(a.Hours, a.Activity);
        if (s.Player.Generation != generation) return new(true, "A sucessão interrompeu a atividade; os benefícios não foram transferidos ao herdeiro.");
        var p = s.Player;
        var l = s.Life;
        p.Energy = Meter(p.Energy + a.Energy);
        p.Hunger = Meter(p.Hunger + a.Hunger);
        p.Health = Meter(p.Health + a.Health);
        p.Stress = Meter(p.Stress + a.Stress);
        p.Social = Meter(p.Social + a.Social);
        l.Fun = Meter(l.Fun + a.Fun);
        l.Hygiene = Meter(l.Hygiene + a.Hygiene);
        l.Hydration = Meter(l.Hydration + a.Hydration);
        l.HomeCare = Meter(l.HomeCare + a.Home);
        l.Purpose = Meter(l.Purpose + (a.Skill.Length > 0 ? 3 : 1));
        p.Happiness = Meter(p.Happiness + Math.Min(4, a.Fun / 8));
        l.LastCompleted[a.Id] = Clock(s);
        if (a.Skill.Length > 0)
        {
            var before = l.PracticeHours.GetValueOrDefault(a.Skill);
            var after = before + a.Hours;
            l.PracticeHours[a.Skill] = after;
            var gained = after / 8 - before / 8;
            p.Skills[a.Skill] = Math.Min(100, Math.Max(1, p.Skills.GetValueOrDefault(a.Skill, 1)) + gained);
            if (gained > 0) Remember(s, $"Evolução em {a.Skill}: nível {p.Skills[a.Skill]}.");
        }
        if (id == "family")
        {
            var partner = s.Citizens.FirstOrDefault(c => c.Alive && c.Id == p.PartnerCitizenId);
            if (partner is not null)
            {
                partner.PlayerAffinity = Meter(partner.PlayerAffinity + 5);
                partner.PlayerTrust = Meter(partner.PlayerTrust + 4);
                partner.LastPlayerInteractionDay = s.CurrentDay;
            }
        }
        Remember(s, $"{a.Name} • {a.Hours}h • Cr$ {a.Cost:N0}. {a.Description}");
        CheckMilestones(s);
        return new(true, $"{a.Name} concluído. {a.Description}");
    }

    public static void Tick(GameState s)
    {
        Initialize(s);
        var l = s.Life;
        var elapsed = Math.Max(0, Clock(s) - l.LastHour);
        l.LastHour = Clock(s);
        if (elapsed == 0) return;
        l.Hydration = Meter(l.Hydration - elapsed * 1.2m);
        l.Hygiene = Meter(l.Hygiene - elapsed * 0.65m);
        l.HomeCare = Meter(l.HomeCare - elapsed * 0.18m);
        l.Fun = Meter(l.Fun - elapsed * 0.45m);
        l.Purpose = Meter(l.Purpose - elapsed * 0.10m);
        // Moderate, bounded consequences; new needs cannot kill a legacy save on loading.
        if (l.Hydration < 15) s.Player.Energy = Meter(s.Player.Energy - Math.Min(8, elapsed * 0.3m));
        if (l.Hygiene < 20 || l.HomeCare < 20) s.Player.Stress = Meter(s.Player.Stress + Math.Min(4, elapsed * 0.15m));
        if (l.Fun < 15) s.Player.Happiness = Meter(s.Player.Happiness - Math.Min(3, elapsed * 0.1m));
        var week = s.CurrentDay / 7;
        if (s.CurrentDay >= 2 && l.PendingEvent is null && l.LastEventWeek < week)
        {
            l.PendingEvent = l.HomeCare < 45 ? "home" : s.Player.Stress > 55 ? "rest" : "community";
            l.LastEventWeek = week;
            Remember(s, EventText(l.PendingEvent));
        }
        CheckMilestones(s);
    }

    public static string EventText(string? id) => id switch
    {
        "home" => "A casa precisa de atenção. Fazer reparos agora ou adiar?",
        "rest" => "Uma semana exigente: reservar tempo para descansar?",
        "community" => "O bairro organiza uma ação comunitária. Participar?",
        _ => "Nenhum convite pendente."
    };
    public static string EventAction(string? id) => id switch { "home" => "repair", "rest" => "rest", _ => "volunteer" };
    public static LifeResult ResolveLifeEvent(this SimulationEngine engine, bool accept)
    {
        var l = engine.State.Life;
        if (l.PendingEvent is null) return new(false, "Nenhum convite pendente.");
        var id = l.PendingEvent;
        if (accept)
        {
            var result = engine.PerformEverydayAction(EventAction(id));
            if (!result.Success) return result;
        }
        else Remember(engine.State, "Você decidiu não aceitar: " + EventText(id));
        l.PendingEvent = null;
        return new(true, accept ? "Você participou; tempo, custo e efeitos foram aplicados." : "Convite recusado. Seu tempo continua livre.");
    }

    public static LifeResult RunLifeRoutine(this SimulationEngine engine, string routine)
    {
        string[] steps = routine switch
        {
            "Manhã" => ["water", "shower", "cook"],
            "Casa e descanso" => ["clean", "read", "rest"],
            "Criatividade" => ["paint", "music", "meditate"],
            _ => []
        };
        if (steps.Length == 0) return new(false, "Rotina desconhecida.");
        var completed = 0;
        foreach (var id in steps)
        {
            var result = engine.PerformEverydayAction(id);
            if (!result.Success) return new(false, $"Rotina pausada após {completed}/{steps.Length}: {result.Message}");
            completed++;
        }
        return new(true, $"Rotina {routine} concluída: {completed} atividades, com tempo e custos aplicados.");
    }

    public static string Suggestion(GameState s)
    {
        if (s.Life.Hydration < 30) return "water";
        if (s.Player.Energy < 25) return "rest";
        if (s.Player.Hunger > 65) return "cook";
        if (s.Life.Hygiene < 35) return "shower";
        if (s.Player.Health < 50 && s.CurrentHour is >= 8 and <= 15 && s.Player.Cash >= 90) return "doctor";
        if (s.Player.Stress > 65) return "meditate";
        if (s.Life.HomeCare < 40) return "clean";
        if (s.Life.Fun < 35) return "read";
        return s.Life.Aspiration switch { "Criatividade" => "paint", "Comunidade" => "volunteer", _ => "budget" };
    }

    public static void Remember(GameState s, string summary)
    {
        s.Life.Journal.Add(new(s.CurrentDay, s.CurrentHour, summary));
        if (s.Life.Journal.Count > 64) s.Life.Journal.RemoveRange(0, s.Life.Journal.Count - 64);
    }

    private static void CheckMilestones(GameState s)
    {
        var l = s.Life;
        void Unlock(string id, bool condition)
        {
            if (!condition || l.Milestones.Contains(id)) return;
            l.Milestones.Add(id);
            l.Purpose = Meter(l.Purpose + 10);
            Remember(s, "Marco de vida: " + id);
        }
        Unlock("Rotina variada", l.LastCompleted.Count >= 6);
        Unlock("Aprendiz dedicado", l.PracticeHours.Values.Any(h => h >= 24));
        Unlock("Casa bem cuidada", l.HomeCare >= 90 && l.LastCompleted.ContainsKey("clean"));
        Unlock("Artista em formação", l.PracticeHours.GetValueOrDefault("Artes") + l.PracticeHours.GetValueOrDefault("Música") >= 24);
        Unlock("Presença na comunidade", l.LastCompleted.ContainsKey("volunteer") && l.PracticeHours.GetValueOrDefault("Comunicação") >= 12);
    }

    public static void Validate(GameState s)
    {
        var l = s.Life ?? throw new InvalidDataException("Estado de cotidiano ausente.");
        if (new[] { l.Hydration, l.Hygiene, l.HomeCare, l.Fun, l.Purpose }.Any(v => v < 0 || v > 100))
            throw new InvalidDataException("Necessidade de cotidiano fora de 0–100.");
        if (l.Journal.Count > 64 || l.PracticeHours.Any(x => x.Value < 0) || l.LastHour > Clock(s))
            throw new InvalidDataException("Estado de cotidiano inconsistente.");
    }
}
