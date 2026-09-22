using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class Main
{
    private int _everydayActionIndex;
    private int _routineIndex;

    private void BuildEverydayLife()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        var life = s.Life;
        AddSidebarTitle("COTIDIANO", "Pequenas escolhas, tempo limitado e uma história sua.", "life");
        var needs = MakePanel(_panel2, 10);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        needs.AddChild(box);
        box.AddChild(MakeProgressStat("Hidratação", life.Hydration, life.Hydration < 30 ? _danger : _accent));
        box.AddChild(MakeProgressStat("Higiene", life.Hygiene, life.Hygiene < 30 ? _danger : _accent));
        box.AddChild(MakeProgressStat("Casa cuidada", life.HomeCare, _gold));
        box.AddChild(MakeProgressStat("Diversão", life.Fun, _success));
        box.AddChild(MakeProgressStat("Propósito", life.Purpose, _accent));
        _sidebar.AddChild(needs);
        _sidebar.AddChild(MakeLabel("Valores baixos afetam energia, estresse ou felicidade. As atividades consomem horas enquanto a cidade continua vivendo.", 11, _muted));

        var suggested = EverydayLife.Actions.First(a => a.Id == EverydayLife.Suggestion(s));
        _sidebar.AddChild(MakeInfoCard("SUGESTÃO PARA AGORA", suggested.Name, suggested.Description, _accent));
        var focus = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        string[] aspirations = ["Equilíbrio", "Criatividade", "Comunidade"];
        foreach (var aspiration in aspirations) focus.AddItem(aspiration);
        focus.Select(Math.Max(0, Array.IndexOf(aspirations, life.Aspiration)));
        focus.ItemSelected += index => { life.Aspiration = aspirations[(int)index]; RefreshAll(); };
        _sidebar.AddChild(MakeLabel("Seu foco orienta sugestões; você mantém o controle.", 10, _muted));
        _sidebar.AddChild(focus);

        _sidebar.AddChild(MakeSection("ESCOLHA SUA PRÓXIMA ATIVIDADE"));
        var options = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var action in EverydayLife.Actions) options.AddItem(action.Name);
        options.Select(_everydayActionIndex);
        options.ItemSelected += index => { _everydayActionIndex = (int)index; RefreshAll(); };
        _sidebar.AddChild(options);
        var chosen = EverydayLife.Actions[_everydayActionIndex];
        _sidebar.AddChild(MakeInfoCard(chosen.Category.ToUpperInvariant(), $"{chosen.Hours}h • Cr$ {chosen.Cost:N0}", chosen.Description, _text));
        var reason = EverydayLife.Unavailable(s, chosen);
        var execute = MakeButton("FAZER ATIVIDADE", true);
        execute.Disabled = reason.Length > 0;
        execute.Pressed += () => { var result = _sim.PerformEverydayAction(chosen.Id); SetStatus(result.Message); RefreshAll(); };
        _sidebar.AddChild(execute);
        if (reason.Length > 0) _sidebar.AddChild(MakeLabel(reason, 11, _gold));

        _sidebar.AddChild(MakeSection("ROTINAS EM UM CLIQUE"));
        string[] routines = ["Manhã", "Casa e descanso", "Criatividade"];
        var routine = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var item in routines) routine.AddItem(item);
        routine.Select(_routineIndex);
        routine.ItemSelected += index => { _routineIndex = (int)index; };
        _sidebar.AddChild(routine);
        _sidebar.AddChild(MakeLabel("Manhã: água, banho, refeição (4h / Cr$ 26). Casa: limpeza, leitura, descanso (6h / Cr$ 8). Criatividade: pintura, música, meditação (5h / Cr$ 20). A rotina para se uma ação ficar indisponível; ações concluídas são mantidas.", 10, _muted));
        var run = MakeButton("EXECUTAR ROTINA", false);
        run.Pressed += () => { SetStatus(_sim.RunLifeRoutine(routines[_routineIndex]).Message); RefreshAll(); };
        _sidebar.AddChild(run);

        if (life.PendingEvent is not null)
        {
            _sidebar.AddChild(MakeSection("UMA ESCOLHA NO SEU DIA"));
            var eventAction = EverydayLife.Actions.First(a => a.Id == EverydayLife.EventAction(life.PendingEvent));
            _sidebar.AddChild(MakeLabel(EverydayLife.EventText(life.PendingEvent), 12, _text));
            _sidebar.AddChild(MakeLabel($"Participar: {eventAction.Hours}h • Cr$ {eventAction.Cost:N0}. Recusar não consome tempo.", 10, _muted));
            var row = new HBoxContainer();
            var accept = MakeButton("PARTICIPAR", true);
            accept.Disabled = EverydayLife.Unavailable(s, eventAction).Length > 0;
            accept.TooltipText = EverydayLife.Unavailable(s, eventAction);
            accept.Pressed += () => { SetStatus(_sim.ResolveLifeEvent(true).Message); RefreshAll(); };
            var decline = MakeButton("RECUSAR", false);
            decline.Pressed += () => { SetStatus(_sim.ResolveLifeEvent(false).Message); RefreshAll(); };
            row.AddChild(accept); row.AddChild(decline); _sidebar.AddChild(row);
        }
        _sidebar.AddChild(MakeSection("PROJETOS E MARCOS"));
        _sidebar.AddChild(MakeLabel("A cada 8h de prática: +1 nível na competência, até 100. Marcos: 6 atividades diferentes; 24h numa habilidade; casa ≥90 após limpeza; 24h de artes/música; 12h de comunicação com voluntariado.", 10, _muted));
        foreach (var skill in life.PracticeHours.OrderByDescending(x => x.Value).Take(5))
            _sidebar.AddChild(MakeLabel($"{skill.Key}: {skill.Value}h • próximo nível em {8 - skill.Value % 8}h", 11, _text));
        foreach (var milestone in life.Milestones)
            _sidebar.AddChild(MakeLabel("✓ " + milestone, 11, _success));
        _sidebar.AddChild(MakeSection("SEU DIÁRIO"));
        if (life.Journal.Count == 0) _sidebar.AddChild(MakeLabel("Suas primeiras escolhas aparecerão aqui.", 11, _muted));
        foreach (var memory in life.Journal.TakeLast(6).Reverse())
            _sidebar.AddChild(MakeLabel($"Dia {memory.Day} • {memory.Hour:00}h — {memory.Summary}", 11, _muted));
    }
}
