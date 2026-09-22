namespace Metropole.Sim;

// Fixed-size utility evaluation. No renderer, network, LLM or per-agent scene nodes.
public static class CitizenDecisions
{
    public static string Choose(GameState state, CitizenState c)
    {
        var hour = state.CurrentHour;
        var weekend = state.CurrentDay % 7 is 5 or 6;
        var raining = state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase);
        var best = "Em casa";
        var reason = "Tempo livre e conforto em casa";
        var score = 23m + (1 - c.Sociability) * 12m;
        void Consider(string activity, decimal utility, string why)
        {
            // Hysteresis reduces pointless switching when two activities have similar utility.
            if (activity == c.CurrentActivity) utility += 5;
            if (utility <= score) return;
            best = activity;
            score = utility;
            reason = why;
        }
        Consider("Dormindo", (100 - c.Energy) * 0.95m + (hour < 6 || hour >= 23 ? 65 : 0), "Recuperar energia e respeitar o descanso noturno");
        if (c.Energy < 12) Consider("Dormindo", 150, "Exaustão: descansar tem prioridade sobre compromissos");
        if (!weekend && c.AgeYears is >= 6 and < 18 && hour is >= 8 and < 15)
            Consider("Estudando", 110, "Compromisso escolar em dia útil");
        if (c.EmployedCompanyId is not null && c.AgeYears >= 18 && hour is >= 8 and < 17 && !weekend)
            Consider("Trabalhando", 80 + c.Discipline * 20, "Expediente de trabalho e responsabilidade profissional");
        if (hour is 7 or 17 && !weekend && (c.EmployedCompanyId is not null || c.AgeYears is >= 6 and < 18))
            Consider("Deslocando-se", 95, "Deslocamento para o compromisso ou de volta para casa");
        if (hour is >= 9 and <= 21)
        {
            Consider("Lazer", c.Stress * 0.65m + (100 - c.Happiness) * 0.3m + (weekend ? 18 : 0) - (raining ? 25 : 0),
                "Aliviar estresse e recuperar felicidade; clima altera a atratividade do passeio");
            Consider("Socializando", c.Sociability * 38 + (100 - c.Happiness) * 0.35m + (c.IsPlayerPartner ? 6 : 0),
                "Buscar companhia conforme a personalidade e o estado emocional");
            if (c.AgeYears >= 18)
                Consider("Estudando", c.Ambition * 38 + (c.EmployedCompanyId is null ? 14 : 0),
                    "Ambição e preparação para oportunidades de carreira");
            if (!raining && c.Energy > 45 && c.AgeYears >= 12)
                Consider("Exercitando-se", c.Discipline * 27 + c.Stress * 0.3m + (weekend ? 12 : 0),
                    "Disciplina, energia disponível e necessidade de reduzir tensão");
        }
        c.DecisionReason = reason;
        return best;
    }
}
