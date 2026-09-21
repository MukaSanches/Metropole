using Godot;
using Metropole.Sim;
using System.IO;

namespace Metropole.Game;

public partial class Main : Control
{
    private enum SidebarMode { Visao, Carreira, Mercado, Empresas, Historico, Ajuda }

    private SimulationEngine? _sim;
    private CityView? _cityView;
    private VBoxContainer? _sidebar;
    private Label? _dateLabel;
    private Label? _cashLabel;
    private Label? _jobLabel;
    private Label? _statusLabel;
    private SidebarMode _mode = SidebarMode.Visao;
    private double _speed;
    private double _tickAccumulator;
    private readonly Color _bg = new(0.035f, 0.05f, 0.075f);
    private readonly Color _panel = new(0.065f, 0.085f, 0.12f);
    private readonly Color _muted = new(0.62f, 0.69f, 0.76f);
    private readonly Color _accent = new(0.22f, 0.72f, 0.78f);
    private readonly Color _gold = new(0.93f, 0.67f, 0.24f);

    private string SavePath => Path.Combine(OS.GetUserDataDir(), "save-1.json");

    public override void _Ready()
    {
        SetProcess(true);
        ShowStartScreen();
    }

    public override void _Process(double delta)
    {
        if (_sim is null || _speed <= 0) return;
        _tickAccumulator += delta * _speed;
        while (_tickAccumulator >= 1.0)
        {
            _tickAccumulator -= 1.0;
            AdvanceOneDay(false);
        }
    }

    private void ShowStartScreen()
    {
        _sim = null;
        _speed = 0;
        ClearNode(this);
        AddBackground();

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = MakePanel();
        panel.CustomMinimumSize = new Vector2(620, 0);
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 42);
        margin.AddThemeConstantOverride("margin_right", 42);
        margin.AddThemeConstantOverride("margin_top", 36);
        margin.AddThemeConstantOverride("margin_bottom", 36);
        panel.AddChild(margin);

        var box = new VBoxContainer();\n        box.AddThemeConstantOverride("separation", 14);
        margin.AddChild(box);

        var title = MakeLabel("METRÓPOLE ∞", 42, _accent);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);

        var sub = MakeLabel("MOTOR DE SOCIEDADE JOGÁVEL", 14, _muted);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(sub);

        box.AddChild(MakeSpacer(12));
        box.AddChild(MakeLabel("Crie uma cidade que continua vivendo sem depender de você.", 17, Colors.White));

        var name = new LineEdit { PlaceholderText = "Nome do personagem", Text = "Cidadão" };
        name.CustomMinimumSize = new Vector2(0, 46);
        box.AddChild(name);

        var seed = new LineEdit { PlaceholderText = "Seed (ex.: 20260921)" };
        seed.CustomMinimumSize = new Vector2(0, 46);
        box.AddChild(seed);

        var newGame = MakeButton("CRIAR NOVO MUNDO", true);
        newGame.Pressed += () =>
        {
            var chosenSeed = long.TryParse(seed.Text.Trim(), out var parsed) ? parsed : DateTime.UtcNow.Ticks;
            try
            {
                _sim = new SimulationEngine(WorldGenerator.Generate(chosenSeed, name.Text));
                BuildGameScreen();
                SetStatus($"Mundo criado com seed {chosenSeed}.");
            }
            catch (Exception ex)
            {
                SetStartError(box, ex.Message);
            }
        };
        box.AddChild(newGame);

        if (File.Exists(SavePath) || File.Exists(SavePath + ".bak"))
        {
            var continueButton = MakeButton("CONTINUAR SAVE", false);
            continueButton.Pressed += () =>
            {
                try
                {
                    _sim = new SimulationEngine(SaveStore.Load(SavePath));
                    BuildGameScreen();
                    SetStatus("Save carregado.");
                }
                catch (Exception ex)
                {
                    SetStartError(box, $"Falha ao carregar: {ex.Message}");
                }
            };
            box.AddChild(continueButton);
        }

        var metrics = ContentCatalog.Metrics;
        box.AddChild(MakeLabel(
            $"{metrics.ProfessionArchetypes:N0} profissões • {metrics.BusinessArchetypes:N0} negócios • {metrics.Products:N0} produtos • {metrics.Events:N0} eventos combináveis",
            13, _muted));
        box.AddChild(MakeLabel("Offline • determinístico por seed • versão 1.0.0", 13, _muted));
    }

    private void BuildGameScreen()
    {
        if (_sim is null) return;
        ClearNode(this);
        AddBackground();

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        root.AddChild(BuildTopBar());

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 12);
        root.AddChild(body);

        body.AddChild(BuildNavigation());

        var mapPanel = MakePanel();
        mapPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mapPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _cityView = new CityView
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(650, 520)
        };
        _cityView.SetEngine(_sim);
        mapPanel.AddChild(_cityView);
        body.AddChild(mapPanel);

        var sidebarPanel = MakePanel();
        sidebarPanel.CustomMinimumSize = new Vector2(390, 0);
        sidebarPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        var scroll = new ScrollContainer();
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        sidebarPanel.AddChild(scroll);
        _sidebar = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _sidebar.AddThemeConstantOverride("separation", 9);
        scroll.AddChild(_sidebar);
        body.AddChild(sidebarPanel);

        _statusLabel = MakeLabel("", 13, _muted);
        root.AddChild(_statusLabel);

        RefreshAll();
    }

    private Control BuildTopBar()
    {
        var panel = MakePanel();
        panel.CustomMinimumSize = new Vector2(0, 74);
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 12);
        panel.AddChild(bar);

        var brand = MakeLabel("METRÓPOLE ∞", 24, _accent);
        brand.CustomMinimumSize = new Vector2(190, 0);
        bar.AddChild(brand);

        _dateLabel = MakeLabel("", 16, Colors.White);
        _dateLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bar.AddChild(_dateLabel);

        _cashLabel = MakeLabel("", 16, _gold);
        bar.AddChild(_cashLabel);

        _jobLabel = MakeLabel("", 14, _muted);
        _jobLabel.CustomMinimumSize = new Vector2(210, 0);
        bar.AddChild(_jobLabel);

        bar.AddChild(MakeSpeedButton("II", 0));
        bar.AddChild(MakeSpeedButton("1×", 1));
        bar.AddChild(MakeSpeedButton("2×", 2));
        bar.AddChild(MakeSpeedButton("4×", 4));
        bar.AddChild(MakeSpeedButton("8×", 8));

        var day = MakeButton("+1 DIA", false);
        day.Pressed += () => AdvanceOneDay(true);
        bar.AddChild(day);

        var save = MakeButton("SALVAR", false);
        save.Pressed += SaveGame;
        bar.AddChild(save);

        return panel;
    }

    private Control BuildNavigation()
    {
        var panel = MakePanel();
        panel.CustomMinimumSize = new Vector2(205, 0);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 7);
        panel.AddChild(box);

        box.AddChild(MakeLabel("CENTRAL", 13, _muted));
        AddNav(box, "Visão geral", SidebarMode.Visao);
        AddNav(box, "Carreira", SidebarMode.Carreira);
        AddNav(box, "Mercado", SidebarMode.Mercado);
        AddNav(box, "Empresas", SidebarMode.Empresas);
        AddNav(box, "Histórico", SidebarMode.Historico);
        AddNav(box, "Como jogar", SidebarMode.Ajuda);

        box.AddChild(MakeSpacer(18));
        var menu = MakeButton("MENU INICIAL", false);
        menu.Pressed += () =>
        {
            SaveGame();
            ShowStartScreen();
        };
        box.AddChild(menu);
        return panel;
    }

    private void AddNav(VBoxContainer box, string text, SidebarMode mode)
    {
        var button = MakeButton(text, false);
        button.Alignment = HorizontalAlignment.Left;
        button.Pressed += () =>
        {
            _mode = mode;
            RefreshSidebar();
        };
        box.AddChild(button);
    }

    private Button MakeSpeedButton(string text, double speed)
    {
        var button = MakeButton(text, false);
        button.CustomMinimumSize = new Vector2(46, 40);
        button.Pressed += () =>
        {
            _speed = speed;
            SetStatus(speed <= 0 ? "Simulação pausada." : $"Velocidade {speed:0}×.");
        };
        return button;
    }

    private void RefreshAll()
    {
        if (_sim is null) return;
        var s = _sim.State;
        _dateLabel!.Text = $"Dia {s.CurrentDay:N0}  •  Ano {1 + s.CurrentDay / 365}";
        _cashLabel!.Text = $"Cr$ {s.Player.Cash:N2}";
        _jobLabel!.Text = s.Player.EmployerCompanyId is int employerId
            ? $"Emprego: {s.Companies.FirstOrDefault(c => c.Id == employerId)?.Name ?? "—"}"
            : "Emprego: procurando";
        _cityView?.QueueRedraw();
        RefreshSidebar();
    }

    private void RefreshSidebar()
    {
        if (_sim is null || _sidebar is null) return;
        ClearNode(_sidebar);
        switch (_mode)
        {
            case SidebarMode.Visao: BuildOverview(); break;
            case SidebarMode.Carreira: BuildCareer(); break;
            case SidebarMode.Mercado: BuildMarket(); break;
            case SidebarMode.Empresas: BuildBusiness(); break;
            case SidebarMode.Historico: BuildHistory(); break;
            case SidebarMode.Ajuda: BuildHelp(); break;
        }
    }

    private void BuildOverview()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        AddSidebarTitle("VISÃO GERAL", "A cidade muda mesmo quando você não age.");

        _sidebar.AddChild(MakeStat("População", s.Population.ToString("N0")));
        _sidebar.AddChild(MakeStat("Empresas ativas", s.OpenCompanies.ToString("N0")));
        _sidebar.AddChild(MakeStat("Desemprego", $"{s.UnemploymentRate:P1}"));
        _sidebar.AddChild(MakeStat("Tesouro agregado", $"Cr$ {s.Treasury:N0}"));
        _sidebar.AddChild(MakeStat("Fome", $"{s.Player.Hunger:0}/100"));
        _sidebar.AddChild(MakeStat("Energia", $"{s.Player.Energy:0}/100"));
        _sidebar.AddChild(MakeStat("Idade / geração", $"{s.Player.AgeYears} anos • G{s.Player.Generation}"));

        var metrics = ContentCatalog.Metrics;
        _sidebar.AddChild(MakeSpacer(8));
        _sidebar.AddChild(MakeLabel("CATÁLOGO SISTÊMICO", 13, _muted));
        _sidebar.AddChild(MakeLabel(
            $"{metrics.ProfessionArchetypes:N0} arquétipos profissionais\\n" +
            $"{metrics.CareerCombinations:N0}+ combinações de carreira\\n" +
            $"{metrics.BusinessArchetypes:N0} arquétipos empresariais\\n" +
            $"{metrics.Products:N0} produtos/serviços\\n" +
            $"{metrics.Resources:N0} recursos/componentes\\n" +
            $"{metrics.Buildings:N0} variações de edifícios\\n" +
            $"{metrics.Skills:N0} competências\\n" +
            $"{metrics.Events:N0} combinações de eventos\\n" +
            $"{metrics.Technologies:N0} tecnologias",
            14, Colors.White));

        var latest = s.History.LastOrDefault();
        if (latest is not null)
        {
            _sidebar.AddChild(MakeSpacer(8));
            _sidebar.AddChild(MakeLabel("ÚLTIMO EVENTO", 13, _muted));
            _sidebar.AddChild(MakeLabel($"{latest.Summary}\\nCausa: {latest.Cause}", 14, Colors.White));
        }
    }

    private void BuildCareer()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        AddSidebarTitle("CARREIRA", "Você pode jogar a vida inteira como funcionário.");

        if (s.Player.EmployerCompanyId is int employerId)
        {
            var employer = s.Companies.FirstOrDefault(c => c.Id == employerId);
            _sidebar.AddChild(MakeLabel($"Cargo atual em:\\n{employer?.Name ?? "Empresa"}", 17, Colors.White));
            _sidebar.AddChild(MakeStat("Salário diário", $"Cr$ {s.Player.DailyWage:N2}"));
            var work = MakeButton("TRABALHAR +1 DIA", true);
            work.Pressed += () =>
            {
                s.Player.Energy = Math.Max(0m, s.Player.Energy - 18m);
                AdvanceOneDay(true);
            };
            _sidebar.AddChild(work);
        }
        else
        {
            _sidebar.AddChild(MakeLabel("Vagas disponíveis", 15, Colors.White));
            foreach (var company in _sim.GetJobBoard())
            {
                var button = MakeButton($"{company.Sector} • {company.Name}\\nCr$ {company.BaseWage * 0.88m:N2}/dia", false);
                button.Alignment = HorizontalAlignment.Left;
                var id = company.Id;
                button.Pressed += () =>
                {
                    if (_sim.AcceptJob(id))
                    {
                        SetStatus("Emprego aceito.");
                        RefreshAll();
                    }
                };
                _sidebar.AddChild(button);
            }
        }
    }

    private void BuildMarket()
    {
        if (_sim is null || _sidebar is null) return;
        AddSidebarTitle("MERCADO", "Preço reage a estoque, produção e demanda.");

        var food = _sim.State.Markets.First(m => m.Family == "Alimentos");
        var buy = MakeButton($"COMPRAR ALIMENTAÇÃO • Cr$ {food.Price:N2}", true);
        buy.Pressed += () =>
        {
            SetStatus(_sim.BuyFood() ? "Compra realizada." : "Compra não realizada: verifique caixa e estoque.");
            RefreshAll();
        };
        _sidebar.AddChild(buy);

        foreach (var market in _sim.State.Markets.OrderByDescending(m => m.DailyDemand).Take(14))
        {
            _sidebar.AddChild(MakeLabel(
                $"{market.Family}\\nCr$ {market.Price:N2} • estoque {market.Stock:N0} • demanda {market.DailyDemand:N1}",
                13, Colors.White));
        }
    }

    private void BuildBusiness()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        AddSidebarTitle("EMPRESAS", "Capital, salários, estoque, preço, crédito e insolvência estão conectados.");

        if (s.Player.BusinessCompanyId is int companyId)
        {
            var company = s.Companies.FirstOrDefault(c => c.Id == companyId);
            if (company is not null)
            {
                _sidebar.AddChild(MakeLabel(company.Name, 19, _gold));
                _sidebar.AddChild(MakeLabel(_sim.ExplainCompany(company.Id), 14, Colors.White));
                _sidebar.AddChild(MakeStat("Funcionários", $"{company.EmployeeIds.Count}/{company.DesiredEmployees}"));
                _sidebar.AddChild(MakeStat("Reputação", $"{company.Reputation:P0}"));
            }
        }
        else
        {
            _sidebar.AddChild(MakeLabel("Abrir uma microempresa exige Cr$ 5.000 de capital real do jogador.", 14, Colors.White));
            var sector = new OptionButton();
            foreach (var item in ContentCatalog.Sectors) sector.AddItem(item.Name);
            sector.CustomMinimumSize = new Vector2(0, 44);
            _sidebar.AddChild(sector);

            var open = MakeButton("ABRIR MICROEMPRESA", true);
            open.Pressed += () =>
            {
                var selected = ContentCatalog.Sectors[sector.Selected].Name;
                SetStatus(_sim.OpenPlayerBusiness(selected)
                    ? $"Empresa aberta no setor {selected}."
                    : "Não foi possível abrir: é preciso ter Cr$ 5.000 e não possuir outra empresa.");
                RefreshAll();
            };
            _sidebar.AddChild(open);
        }

        _sidebar.AddChild(MakeSpacer(10));
        _sidebar.AddChild(MakeLabel("MAIORES EMPRESAS POR CAIXA", 13, _muted));
        foreach (var company in s.Companies.Where(c => c.Open).OrderByDescending(c => c.Cash).Take(8))
            _sidebar.AddChild(MakeLabel($"{company.Name}\\n{company.Sector} • Cr$ {company.Cash:N0}", 13, Colors.White));
    }

    private void BuildHistory()
    {
        if (_sim is null || _sidebar is null) return;
        AddSidebarTitle("HISTÓRICO CAUSAL", "Eventos importantes registram o motivo, não apenas o resultado.");

        foreach (var evt in _sim.State.History.AsEnumerable().Reverse().Take(35))
        {
            _sidebar.AddChild(MakeLabel(
                $"D{evt.Day} • {evt.Kind}\\n{evt.Summary}\\n↳ {evt.Cause}",
                13, evt.Kind == "Falência" ? _gold : Colors.White));
        }
    }

    private void BuildHelp()
    {
        if (_sidebar is null) return;
        AddSidebarTitle("COMO JOGAR", "Os primeiros minutos já atravessam vários sistemas.");
        _sidebar.AddChild(MakeLabel(
            "1. Vá em Carreira e aceite uma vaga.\\n\\n" +
            "2. Trabalhe ou acelere o tempo para receber salário.\\n\\n" +
            "3. Em Mercado, compre alimentação e observe preços/estoques.\\n\\n" +
            "4. Acumule Cr$ 5.000 e abra uma empresa.\\n\\n" +
            "5. Acompanhe contratações, demissões, crédito e falências no Histórico.\\n\\n" +
            "6. Salve quando quiser. O jogo cria backup antes de substituir o save válido.\\n\\n" +
            "7. Use a mesma seed para reproduzir as condições iniciais.",
            15, Colors.White));
    }

    private void AdvanceOneDay(bool manual)
    {
        if (_sim is null) return;
        try
        {
            _sim.AdvanceOneDay();
            if (_sim.State.CurrentDay % 30 == 0) SaveStore.Save(SavePath, _sim.State);
            if (manual) SetStatus($"Dia {_sim.State.CurrentDay} concluído.");
            RefreshAll();
        }
        catch (Exception ex)
        {
            _speed = 0;
            SetStatus($"Simulação pausada por erro: {ex.Message}");
            GD.PrintErr(ex);
        }
    }

    private void SaveGame()
    {
        if (_sim is null) return;
        try
        {
            SaveStore.Save(SavePath, _sim.State);
            SetStatus($"Save confirmado em {SavePath}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao salvar: {ex.Message}");
            GD.PrintErr(ex);
        }
    }

    private void AddSidebarTitle(string title, string subtitle)
    {
        _sidebar!.AddChild(MakeLabel(title, 23, _accent));
        _sidebar.AddChild(MakeLabel(subtitle, 13, _muted));
        _sidebar.AddChild(MakeSpacer(4));
    }

    private Control MakeStat(string label, string value)
    {
        var box = new HBoxContainer();
        var left = MakeLabel(label, 14, _muted);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        box.AddChild(left);
        box.AddChild(MakeLabel(value, 15, Colors.White));
        return box;
    }

    private PanelContainer MakePanel()
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = _panel,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.12f, 0.17f, 0.22f)
        };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private Button MakeButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 44)
        };
        button.AddThemeFontSizeOverride("font_size", 14);
        button.AddThemeColorOverride("font_color", Colors.White);
        var style = new StyleBoxFlat
        {
            BgColor = primary ? new Color(0.10f, 0.46f, 0.50f) : new Color(0.09f, 0.12f, 0.16f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = primary ? _accent : new Color(0.15f, 0.20f, 0.26f)
        };
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style.Duplicate() as StyleBoxFlat ?? style);
        return button;
    }

    private Label MakeLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static Control MakeSpacer(float height) => new Control { CustomMinimumSize = new Vector2(0, height) };

    private void AddBackground()
    {
        var background = new ColorRect { Color = _bg, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);
    }

    private void SetStatus(string text)
    {
        if (_statusLabel is not null) _statusLabel.Text = text;
    }

    private void SetStartError(VBoxContainer box, string text)
    {
        box.AddChild(MakeLabel(text, 13, new Color(1f, 0.45f, 0.38f)));
    }

    private static void ClearNode(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }
}
