using Godot;
using Metropole.Sim;
using System.IO;

namespace Metropole.Game;

public partial class Main : Control
{
    private enum SidebarMode { Visao, Vida, Pessoas, Cidade, Carreira, Mercado, Empresas, Historico, Ajuda }

    private SimulationEngine? _sim;
    private AudioDirector? _audio;
    private CityView? _cityView;
    private PremiumCityView? _premiumCityView;
    private Label? _graphicsBadge;
    private VBoxContainer? _sidebar;
    private Label? _dateLabel;
    private Label? _cashLabel;
    private Label? _jobLabel;
    private Label? _statusLabel;
    private Label? _mapSubtitle;
    private readonly Dictionary<SidebarMode, Button> _nav = new();
    private SidebarMode _mode = SidebarMode.Visao;
    private double _speed;
    private double _tickAccumulator;

    private readonly Color _bg = new(0.025f, 0.045f, 0.070f);
    private readonly Color _panel = new(0.050f, 0.085f, 0.120f);
    private readonly Color _panel2 = new(0.075f, 0.125f, 0.175f);
    private readonly Color _panel3 = new(0.095f, 0.165f, 0.225f);
    private readonly Color _line = new(0.14f, 0.23f, 0.31f);
    private readonly Color _text = new(0.93f, 0.97f, 0.99f);
    private readonly Color _muted = new(0.58f, 0.66f, 0.73f);
    private readonly Color _muted2 = new(0.38f, 0.47f, 0.56f);
    private readonly Color _accent = new(0.24f, 0.84f, 0.82f);
    private readonly Color _gold = new(0.96f, 0.72f, 0.29f);
    private readonly Color _success = new(0.43f, 0.86f, 0.56f);
    private readonly Color _danger = new(1.0f, 0.42f, 0.42f);

    private string SavePath => Path.Combine(OS.GetUserDataDir(), "save-1.json");

    public override void _Ready()
    {
        SetProcess(true);
        GetWindow().MinSize = new Vector2I(1280, 720);
        EnsureAudioDirector();

        var validationRequested =
            OS.GetCmdlineUserArgs().Contains("--validation-run") ||
            string.Equals(System.Environment.GetEnvironmentVariable("METROPOLE_UI_VALIDATION"), "1", StringComparison.Ordinal);

        if (validationRequested)
        {
            GD.Print("METROPOLE_UI_VALIDATION_START");
            CallDeferred(MethodName.RunUiValidation);
            return;
        }

        ShowStartScreen();
    }

    private void EnsureAudioDirector()
    {
        _audio = GetNodeOrNull<AudioDirector>("AudioDirector");
        if (_audio is not null) return;

        _audio = new AudioDirector { Name = "AudioDirector" };
        AddChild(_audio);
    }

    private void RunUiValidation()
    {
        try
        {
            ThirdPartyAssetCatalog.ValidateOrThrow();
            _sim = new SimulationEngine(WorldGenerator.Generate(120260921, "Validação"));
            BuildGameScreen();
            _audio?.SetSimulation(_sim);

            foreach (var mode in Enum.GetValues<SidebarMode>())
            {
                _mode = mode;
                RefreshAll();
            }

            var job = _sim.GetJobBoard(1).FirstOrDefault();
            if (job is not null)
            {
                _sim.AcceptJob(job.Id);
                _sim.WorkShift(8);
            }

            _sim.State.Player.Cash = Math.Max(_sim.State.Player.Cash, 25_000m);
            if (_sim.State.Player.BusinessCompanyId is null)
                _sim.OpenPlayerBusiness(ContentCatalog.Sectors[0].Name);

            if (_sim.State.Player.BusinessCompanyId is int companyId)
            {
                _sim.ConfigureBrand("Metrópole Lab", "A cidade em movimento.");
                _sim.SetPricingStrategy("Premium");
                _sim.AdjustMarketingBudget(25m);
                var company = _sim.State.Companies.First(c => c.Id == companyId);
                company.Cash += 5_000m;
                _sim.InvestInQuality(1_500m);
                _sim.InvestInInnovation(1_500m);
            }

            _sim.Socialize(3);
            _sim.Study(4);
            _sim.AdvanceHours(30);

            foreach (var mode in Enum.GetValues<SidebarMode>())
            {
                _mode = mode;
                RefreshAll();
            }

            if (_premiumCityView is not null)
                _premiumCityView.ValidateLicensedAssetsOrThrow();

            var assets = ThirdPartyAssetCatalog.Stats();
            GD.Print($"METROPOLE_UI_VALIDATION_OK day={_sim.State.CurrentDay} hour={_sim.State.CurrentHour} companies={_sim.State.OpenCompanies} population={_sim.State.Population} cityAssets={assets.CityModels} vehicleAssets={assets.VehicleModels} characterAssets={assets.CharacterModels} uiAudio={assets.UiSounds} animated={_premiumCityView?.AnimatedCharacterInstances ?? 0}");
            GetTree().Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("METROPOLE_UI_VALIDATION_FAILED");
            GD.PrintErr(ex);
            GetTree().Quit(2);
        }
    }

    public override void _Process(double delta)
    {
        if (_sim is null || _speed <= 0) return;
        _tickAccumulator += delta * _speed;
        while (_tickAccumulator >= 1.0)
        {
            _tickAccumulator -= 1.0;
            AdvanceSimulationHour();
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        if (key.Keycode == Key.S && key.CtrlPressed && _sim is not null)
        {
            SaveGame();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Escape && _sim is not null)
        {
            SaveGame();
            ShowStartScreen();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ShowStartScreen()
    {
        _audio?.SetSimulation(null);
        _sim = null;
        _speed = 0;
        _tickAccumulator = 0;
        _nav.Clear();
        ClearNode(this);
        AddBackground();

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 42);
        margin.AddThemeConstantOverride("margin_right", 42);
        margin.AddThemeConstantOverride("margin_top", 34);
        margin.AddThemeConstantOverride("margin_bottom", 34);
        AddChild(margin);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 26);
        margin.AddChild(row);

        var hero = MakePanel(_panel, 22);
        hero.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hero.SizeFlagsVertical = SizeFlags.ExpandFill;
        row.AddChild(hero);

        var heroBox = new VBoxContainer();
        heroBox.AddThemeConstantOverride("separation", 13);
        hero.AddChild(heroBox);

        var logo = MakeIcon("app_icon", 94);
        heroBox.AddChild(logo);
        heroBox.AddChild(MakeLabel("METRÓPOLE ∞", 46, _text, false));
        heroBox.AddChild(MakeLabel("UMA CIDADE QUE NÃO ESPERA POR VOCÊ", 14, _accent, false));
        heroBox.AddChild(MakeSpacer(8));
        heroBox.AddChild(MakeLabel(
            "Construa uma vida dentro de uma economia viva. Trabalhe, mude de bairro, acompanhe mercados, abra empresas e veja milhares de agentes reagindo às mesmas regras.",
            19, _muted));

        var stats = new GridContainer { Columns = 2 };
        stats.AddThemeConstantOverride("h_separation", 10);
        stats.AddThemeConstantOverride("v_separation", 10);
        stats.AddChild(MakeMetricCard("CIDADÃOS", "1.200+", _accent));
        stats.AddChild(MakeMetricCard("EMPRESAS", "180+", _gold));
        stats.AddChild(MakeMetricCard("DISTRITOS", "9", _text));
        stats.AddChild(MakeMetricCard("TEMPO", "INFINITO", _success));
        heroBox.AddChild(stats);
        heroBox.AddChild(MakeSpacer(6));
        heroBox.AddChild(MakeLabel("OFFLINE • SAVE LOCAL • ECONOMIA DETERMINÍSTICA", 11, _muted2, false));

        var setup = MakePanel(_panel, 22);
        setup.CustomMinimumSize = new Vector2(455, 0);
        setup.SizeFlagsVertical = SizeFlags.ExpandFill;
        row.AddChild(setup);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 11);
        setup.AddChild(box);

        box.AddChild(MakeLabel("NOVO MUNDO", 28, _text, false));
        box.AddChild(MakeLabel("Defina seu personagem e inicie uma nova sociedade.", 14, _muted));
        box.AddChild(MakeSpacer(6));

        box.AddChild(MakeLabel("SEU NOME", 10, _muted2, false));
        var name = MakeLineEdit("Nome do personagem", "Cidadão");
        box.AddChild(name);

        box.AddChild(MakeLabel("SEED DO MUNDO", 10, _muted2, false));
        var seed = MakeLineEdit("Ex.: 20260921", "");
        box.AddChild(seed);
        box.AddChild(MakeLabel("Deixe vazio para gerar uma seed automaticamente.", 11, _muted2));

        var newGame = MakeButton("CRIAR NOVO MUNDO", true, "city");
        newGame.CustomMinimumSize = new Vector2(0, 52);
        newGame.Pressed += () =>
        {
            var chosenSeed = long.TryParse(seed.Text.Trim(), out var parsed) ? parsed : DateTime.UtcNow.Ticks;
            try
            {
                _sim = new SimulationEngine(WorldGenerator.Generate(chosenSeed, name.Text));
                _mode = SidebarMode.Visao;
                BuildGameScreen();
                SetStatus($"Mundo criado • seed {chosenSeed}");
            }
            catch (Exception ex)
            {
                box.AddChild(MakeInfoCard("ERRO", "Não foi possível iniciar", ex.Message, _danger));
            }
        };
        box.AddChild(newGame);

        if (File.Exists(SavePath) || File.Exists(SavePath + ".bak"))
        {
            var continueButton = MakeButton("CONTINUAR PARTIDA", false, "overview");
            continueButton.CustomMinimumSize = new Vector2(0, 48);
            continueButton.Pressed += () =>
            {
                try
                {
                    _sim = new SimulationEngine(SaveStore.Load(SavePath));
                    _mode = SidebarMode.Visao;
                    BuildGameScreen();
                    SetStatus("Save carregado com sucesso.");
                }
                catch (Exception ex)
                {
                    box.AddChild(MakeInfoCard("ERRO", "Falha ao carregar", ex.Message, _danger));
                }
            };
            box.AddChild(continueButton);
        }

        box.AddChild(MakeSpacer(6));
        box.AddChild(MakeDivider());
        var metrics = ContentCatalog.Metrics;
        box.AddChild(MakeLabel(
            $"{metrics.ProfessionArchetypes:N0} profissões • {metrics.BusinessArchetypes:N0} negócios\n" +
            $"{metrics.Products:N0} produtos • {metrics.Events:N0} eventos combináveis",
            12, _muted));
        box.AddChild(MakeLabel("METRÓPOLE ∞ 1.4.0", 11, _muted2, false));
    }

    private void BuildGameScreen()
    {
        if (_sim is null) return;

        _audio?.SetSimulation(_sim);
        ClearNode(this);
        _nav.Clear();
        AddBackground();

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 9);
        margin.AddChild(root);
        root.AddChild(BuildTopBar());

        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 9);
        root.AddChild(body);

        body.AddChild(BuildNavigation());
        body.AddChild(BuildMapPanel());
        body.AddChild(BuildSidebar());

        root.AddChild(BuildStatusBar());
        RefreshAll();
    }

    private Control BuildTopBar()
    {
        var panel = MakePanel(_panel, 15);
        panel.CustomMinimumSize = new Vector2(0, 78);
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 10);
        panel.AddChild(bar);

        var brand = new HBoxContainer { CustomMinimumSize = new Vector2(205, 0) };
        brand.AddThemeConstantOverride("separation", 9);
        brand.AddChild(MakeIcon("app_icon", 38));
        var bt = new VBoxContainer();
        bt.AddThemeConstantOverride("separation", 0);
        bt.AddChild(MakeLabel("METRÓPOLE ∞", 19, _text, false));
        bt.AddChild(MakeLabel("SOCIEDADE EM TEMPO REAL", 9, _muted2, false));
        brand.AddChild(bt);
        bar.AddChild(brand);
        bar.AddChild(MakeVSeparator());

        _dateLabel = MakeTopStat(bar, "DATA", 125);
        _cashLabel = MakeTopStat(bar, "PATRIMÔNIO", 170, _gold);
        _jobLabel = MakeTopStat(bar, "ATIVIDADE", 235);

        bar.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var controls = new HBoxContainer();
        controls.AddThemeConstantOverride("separation", 4);
        controls.AddChild(MakeSpeedButton("Ⅱ", 0));
        controls.AddChild(MakeSpeedButton("1×", 1));
        controls.AddChild(MakeSpeedButton("2×", 2));
        controls.AddChild(MakeSpeedButton("4×", 4));
        controls.AddChild(MakeSpeedButton("8×", 8));
        bar.AddChild(controls);

        var day = MakeButton("+1 DIA", false);
        day.CustomMinimumSize = new Vector2(76, 40);
        day.Pressed += () => AdvanceOneDay(true);
        bar.AddChild(day);

        var save = MakeButton("SALVAR", true);
        save.CustomMinimumSize = new Vector2(80, 40);
        save.Pressed += SaveGame;
        bar.AddChild(save);
        return panel;
    }

    private Control BuildNavigation()
    {
        var panel = MakePanel(_panel, 15);
        panel.CustomMinimumSize = new Vector2(210, 0);
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);

        box.AddChild(MakeLabel("CENTRAL", 10, _muted2, false));
        AddNav(box, "Visão geral", SidebarMode.Visao, "overview");
        AddNav(box, "Vida", SidebarMode.Vida, "life");
        AddNav(box, "Pessoas", SidebarMode.Pessoas, "people");
        AddNav(box, "Cidade", SidebarMode.Cidade, "city");
        AddNav(box, "Carreira", SidebarMode.Carreira, "career");
        AddNav(box, "Mercado", SidebarMode.Mercado, "market");
        AddNav(box, "Empresas", SidebarMode.Empresas, "business");
        AddNav(box, "Histórico", SidebarMode.Historico, "history");
        AddNav(box, "Como jogar", SidebarMode.Ajuda, "help");

        box.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        box.AddChild(MakeDivider());

        var save = MakeButton("SALVAR PARTIDA", false);
        save.Pressed += SaveGame;
        box.AddChild(save);

        var menu = MakeButton("MENU INICIAL", false);
        menu.Pressed += () =>
        {
            SaveGame();
            ShowStartScreen();
        };
        box.AddChild(menu);
        return panel;
    }

    private Control BuildMapPanel()
    {
        var panel = MakePanel(_panel, 15);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);

        var head = new HBoxContainer();
        var titles = new VBoxContainer();
        titles.AddThemeConstantOverride("separation", 0);
        titles.AddChild(MakeLabel("CIDADE EM TEMPO REAL", 14, _text, false));
        _mapSubtitle = MakeLabel("", 10, _muted);
        titles.AddChild(_mapSubtitle);
        head.AddChild(titles);
        head.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _graphicsBadge = MakeBadge("RENDER", _accent, new Color(0.07f, 0.20f, 0.22f));
        head.AddChild(_graphicsBadge);
        head.AddChild(MakeBadge("● ONLINE LOCAL", _accent, new Color(0.07f, 0.20f, 0.22f)));
        box.AddChild(head);

        var frame = MakePanel(new Color(0.018f, 0.042f, 0.064f), 11);
        frame.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        frame.SizeFlagsVertical = SizeFlags.ExpandFill;
        frame.ClipContents = true;
        box.AddChild(frame);

        _cityView = null;
        _premiumCityView = null;

        if (GraphicsQuality.UsePremium3D)
        {
            _premiumCityView = new PremiumCityView
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(620, 480)
            };
            _premiumCityView.SetEngine(_sim!);
            frame.AddChild(_premiumCityView);
        }
        else
        {
            _cityView = new CityView
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(620, 480)
            };
            _cityView.SetEngine(_sim!);
            frame.AddChild(_cityView);
        }

        var footer = new HBoxContainer();
        footer.AddThemeConstantOverride("separation", 10);
        footer.AddChild(MakeLegend(_accent, "Seu bairro"));
        footer.AddChild(MakeLegend(_gold, "Sua empresa"));
        footer.AddChild(MakeLegend(_muted, "Economia local"));
        footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        if (_premiumCityView is not null)
        {
            var graphicsMode = MakeButton($"GRÁFICOS: {_premiumCityView.QualityModeLabel}", false);
            graphicsMode.CustomMinimumSize = new Vector2(150, 34);
            graphicsMode.Pressed += () =>
            {
                _premiumCityView?.CycleQualityMode();
                if (_premiumCityView is not null)
                {
                    graphicsMode.Text = $"GRÁFICOS: {_premiumCityView.QualityModeLabel}";
                    SetStatus($"Perfil gráfico: {_premiumCityView.QualityModeLabel}. AUTO adapta densidade ao desempenho.");
                    if (_graphicsBadge is not null) _graphicsBadge.Text = _premiumCityView.Diagnostics;
                }
            };
            footer.AddChild(graphicsMode);
        }

        footer.AddChild(MakeLabel(
            GraphicsQuality.UsePremium3D
                ? "2.5D • MultiMesh • AUTO adaptativo • zoom/pan"
                : "fallback leve • dia/noite • clima • tráfego",
            10, _muted2, false));
        box.AddChild(footer);
        return panel;
    }

    private Control BuildSidebar()
    {
        var panel = MakePanel(_panel, 15);
        panel.CustomMinimumSize = new Vector2(360, 0);
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddChild(scroll);

        _sidebar = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _sidebar.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_sidebar);
        return panel;
    }

    private Control BuildStatusBar()
    {
        var panel = MakePanel(new Color(0.035f, 0.070f, 0.095f), 9);
        panel.CustomMinimumSize = new Vector2(0, 34);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);
        row.AddChild(MakeBadge("●", _success, new Color(0.05f, 0.16f, 0.09f)));
        _statusLabel = MakeLabel("Cidade pronta.", 10, _muted);
        _statusLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(_statusLabel);
        row.AddChild(MakeLabel("Ctrl+S salvar • Esc menu", 10, _muted2, false));
        return panel;
    }

    private void AddNav(VBoxContainer box, string text, SidebarMode mode, string icon)
    {
        var button = MakeButton(text, false, icon);
        button.Alignment = HorizontalAlignment.Left;
        button.CustomMinimumSize = new Vector2(0, 44);
        button.Pressed += () =>
        {
            _mode = mode;
            UpdateNavState();
            RefreshSidebar();
        };
        _nav[mode] = button;
        box.AddChild(button);
    }

    private void UpdateNavState()
    {
        foreach (var item in _nav)
            ApplyButtonStyle(item.Value, item.Key == _mode, item.Key == _mode);
    }

    private Button MakeSpeedButton(string text, double speed)
    {
        var button = MakeButton(text, false);
        button.CustomMinimumSize = new Vector2(37, 38);
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
        var employer = s.Player.EmployerCompanyId is int employerId
            ? s.Companies.FirstOrDefault(c => c.Id == employerId)
            : null;

        _dateLabel!.Text = $"D{s.CurrentDay:N0} • {s.CurrentHour:00}:00 • Ano {1 + s.CurrentDay / 365}";
        _cashLabel!.Text = $"Cr$ {s.Player.Cash:N2}";
        _jobLabel!.Text = employer?.Name ?? "Em busca de trabalho";
        _mapSubtitle!.Text = $"{s.Population:N0} hab. • {s.OpenCompanies:N0} empresas • {s.Weather} {s.TemperatureC:0}°C • confiança {s.CityConfidence:P0}";
        _cityView?.QueueRedraw();
        _premiumCityView?.RefreshFromSimulation();
        if (_graphicsBadge is not null)
        {
            _graphicsBadge.Text = _premiumCityView is not null
                ? _premiumCityView.Diagnostics
                : $"{GraphicsQuality.RenderingMethod}/{GraphicsQuality.RenderingDriver} • LOW • 2D";
        }
        UpdateNavState();
        RefreshSidebar();
    }

    private void RefreshSidebar()
    {
        if (_sim is null || _sidebar is null) return;
        ClearNode(_sidebar);
        switch (_mode)
        {
            case SidebarMode.Visao: BuildOverview(); break;
            case SidebarMode.Vida: BuildLife(); break;
            case SidebarMode.Pessoas: BuildPeople(); break;
            case SidebarMode.Cidade: BuildCity(); break;
            case SidebarMode.Carreira: BuildCareer(); break;
            case SidebarMode.Mercado: BuildMarket(); break;
            case SidebarMode.Empresas: BuildBusinessDeep(); break;
            case SidebarMode.Historico: BuildHistory(); break;
            case SidebarMode.Ajuda: BuildHelp(); break;
        }
    }

    private void BuildOverview()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        AddSidebarTitle("VISÃO GERAL", "Seu retrato financeiro e o pulso da cidade.", "overview");

        var player = MakePanel(_panel2, 11);
        var p = new VBoxContainer();
        p.AddThemeConstantOverride("separation", 6);
        player.AddChild(p);
        p.AddChild(MakeLabel(s.Player.Name, 19, _text, false));
        p.AddChild(MakeLabel($"{s.Player.AgeYears} anos • G{s.Player.Generation} • {s.Districts.First(d => d.Id == s.Player.DistrictId).Name}", 11, _muted));
        p.AddChild(MakeDivider());
        p.AddChild(MakeProgressStat("Energia", s.Player.Energy, _accent));
        p.AddChild(MakeProgressStat("Fome", s.Player.Hunger, s.Player.Hunger > 70 ? _danger : _gold));
        _sidebar.AddChild(player);

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 7);
        grid.AddThemeConstantOverride("v_separation", 7);
        grid.AddChild(MakeMetricCard("POPULAÇÃO", s.Population.ToString("N0"), _text));
        grid.AddChild(MakeMetricCard("EMPRESAS", s.OpenCompanies.ToString("N0"), _text));
        grid.AddChild(MakeMetricCard("DESEMPREGO", s.UnemploymentRate.ToString("P1"), _gold));
        grid.AddChild(MakeMetricCard("TESOURO", $"Cr$ {s.Treasury / 1_000_000m:N1} mi", _accent));
        _sidebar.AddChild(grid);

        var latest = s.History.LastOrDefault();
        if (latest is not null)
        {
            _sidebar.AddChild(MakeSection("ÚLTIMO ACONTECIMENTO"));
            _sidebar.AddChild(MakeInfoCard($"DIA {latest.Day} • {latest.Kind.ToUpperInvariant()}", latest.Summary, latest.Cause, latest.Kind == "Falência" ? _danger : _accent));
        }

        _sidebar.AddChild(MakeSection("AÇÃO RÁPIDA"));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 7);
        var rest = MakeButton("DESCANSAR +1 DIA", false);
        rest.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        rest.Pressed += () => AdvanceOneDay(true);
        row.AddChild(rest);
        var market = MakeButton("MERCADO", true);
        market.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        market.Pressed += () =>
        {
            _mode = SidebarMode.Mercado;
            UpdateNavState();
            RefreshSidebar();
        };
        row.AddChild(market);
        _sidebar.AddChild(row);
    }

    private void BuildCity()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        var current = s.Districts.First(d => d.Id == s.Player.DistrictId);
        AddSidebarTitle("CIDADE", "Bairros alteram aluguel, riqueza e logística.", "city");
        _sidebar.AddChild(MakeInfoCard("SEU BAIRRO", current.Name,
            $"Aluguel {current.RentIndex:0.00}× • riqueza {current.WealthIndex:0.00}× • logística {current.LogisticsIndex:0.00}×", _accent));

        foreach (var district in s.Districts.OrderByDescending(d => d.WealthIndex))
        {
            var companies = s.Companies.Count(c => c.Open && c.DistrictId == district.Id);
            var residents = s.Citizens.Count(c => c.Alive && c.DistrictId == district.Id);
            var card = MakePanel(district.Id == s.Player.DistrictId ? new Color(0.07f, 0.24f, 0.25f) : _panel2, 10);
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 5);
            card.AddChild(box);

            var top = new HBoxContainer();
            var title = MakeLabel(district.Name, 14, _text, false);
            title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            top.AddChild(title);
            top.AddChild(MakeBadge(district.Id == s.Player.DistrictId ? "VOCÊ" : $"{companies} EMP.", district.Id == s.Player.DistrictId ? _accent : _muted, _panel3));
            box.AddChild(top);
            box.AddChild(MakeLabel($"{residents:N0} moradores • aluguel {district.RentIndex:0.00}× • logística {district.LogisticsIndex:0.00}×", 10, _muted));

            if (district.Id != s.Player.DistrictId)
            {
                var cost = _sim.GetMoveCost(district.Id);
                var move = MakeButton($"MUDAR • Cr$ {cost:N0}", false);
                var id = district.Id;
                move.Pressed += () =>
                {
                    SetStatus(_sim.MovePlayerDistrict(id) ? $"Mudança para {district.Name} concluída." : "Caixa insuficiente para mudar.");
                    RefreshAll();
                };
                box.AddChild(move);
            }
            _sidebar.AddChild(card);
        }
    }

    private void BuildCareer()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        AddSidebarTitle("CARREIRA", "Trabalhe, descanse e construa patrimônio.", "career");

        if (s.Player.EmployerCompanyId is int employerId)
        {
            var employer = s.Companies.FirstOrDefault(c => c.Id == employerId);
            _sidebar.AddChild(MakeInfoCard("EMPREGO ATUAL", employer?.Name ?? "Empresa",
                $"{employer?.Sector ?? "Setor"} • Cr$ {s.Player.DailyWage:N2}/dia", _accent));
            _sidebar.AddChild(MakeProgressStat("Energia para trabalhar", s.Player.Energy, s.Player.Energy < 30 ? _danger : _accent));

            var work = MakeButton("TRABALHAR 8H", true, "career");
            work.CustomMinimumSize = new Vector2(0, 50);
            work.Disabled = s.Player.Energy < 18m;
            work.Pressed += () =>
            {
                _sim.WorkShift(8);
                SetStatus("Turno de 8 horas concluído.");
                RefreshAll();
            };
            _sidebar.AddChild(work);

            var rest = MakeButton("DORMIR 8H", false);
            rest.Pressed += () =>
            {
                _sim.Sleep(8);
                SetStatus("Você dormiu 8 horas.");
                RefreshAll();
            };
            _sidebar.AddChild(rest);

            var quit = MakeButton("PEDIR DEMISSÃO", false);
            quit.Pressed += () =>
            {
                if (_sim.LeaveJob())
                {
                    SetStatus("Você deixou o emprego atual.");
                    RefreshAll();
                }
            };
            _sidebar.AddChild(quit);
        }
        else
        {
            _sidebar.AddChild(MakeInfoCard("SEM EMPREGO", "Você está disponível",
                "Escolha uma vaga. Salário e reputação variam por empresa.", _gold));
            _sidebar.AddChild(MakeSection("MELHORES VAGAS"));

            foreach (var company in _sim.GetJobBoard(10))
            {
                var card = MakePanel(_panel2, 10);
                var box = new VBoxContainer();
                box.AddThemeConstantOverride("separation", 5);
                card.AddChild(box);

                var head = new HBoxContainer();
                var name = MakeLabel(company.Name, 13, _text);
                name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                head.AddChild(name);
                head.AddChild(MakeBadge($"Cr$ {company.BaseWage * 0.88m:N0}/d", _gold, new Color(0.22f, 0.17f, 0.07f)));
                box.AddChild(head);
                box.AddChild(MakeLabel($"{company.Sector} • reputação {company.Reputation:P0} • {company.EmployeeIds.Count}/{company.DesiredEmployees} ocupadas", 10, _muted));

                var accept = MakeButton("ACEITAR VAGA", false);
                var id = company.Id;
                accept.Pressed += () =>
                {
                    if (_sim.AcceptJob(id))
                    {
                        SetStatus($"Contratado pela {company.Name}.");
                        RefreshAll();
                    }
                };
                box.AddChild(accept);
                _sidebar.AddChild(card);
            }
        }
    }

    private void BuildMarket()
    {
        if (_sim is null || _sidebar is null) return;
        AddSidebarTitle("MERCADO", "Oferta, demanda e estoque alteram os preços.", "market");

        var food = _sim.State.Markets.First(m => m.Family == "Alimentos");
        var foodCard = MakePanel(new Color(0.07f, 0.19f, 0.14f), 11);
        var f = new VBoxContainer();
        f.AddThemeConstantOverride("separation", 6);
        foodCard.AddChild(f);
        f.AddChild(MakeLabel("ALIMENTAÇÃO", 10, _success, false));
        f.AddChild(MakeLabel($"Cr$ {food.Price:N2}", 25, _text, false));
        f.AddChild(MakeLabel($"Estoque {food.Stock:N0} • demanda {food.DailyDemand:N1}", 10, _muted));

        var buyRow = new HBoxContainer();
        buyRow.AddThemeConstantOverride("separation", 6);
        foreach (var qty in new[] { 1, 3, 5 })
        {
            var b = MakeButton(qty == 1 ? "COMPRAR 1" : $"COMPRAR {qty}", qty == 1);
            b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var amount = qty;
            b.Pressed += () =>
            {
                var bought = _sim.BuyFood(amount);
                SetStatus(bought > 0 ? $"{bought} alimentação(ões) comprada(s)." : "Compra não realizada.");
                RefreshAll();
            };
            buyRow.AddChild(b);
        }
        f.AddChild(buyRow);
        _sidebar.AddChild(foodCard);

        _sidebar.AddChild(MakeSection("INDICADORES"));
        foreach (var market in _sim.State.Markets.OrderByDescending(m => m.DailyDemand).Take(12))
        {
            var rising = market.DailyDemand > market.DailySupply;
            var card = MakePanel(_panel2, 9);
            var row = new HBoxContainer();
            card.AddChild(row);
            var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            left.AddThemeConstantOverride("separation", 0);
            left.AddChild(MakeLabel(market.Family, 12, _text, false));
            left.AddChild(MakeLabel($"estoque {market.Stock:N0} • demanda {market.DailyDemand:N1}", 9, _muted));
            row.AddChild(left);
            var right = new VBoxContainer();
            right.AddThemeConstantOverride("separation", 0);
            var price = MakeLabel($"Cr$ {market.Price:N2}", 12, _text, false);
            price.HorizontalAlignment = HorizontalAlignment.Right;
            right.AddChild(price);
            var trend = MakeLabel(rising ? "DEMANDA ↑" : "OFERTA ↑", 9, rising ? _gold : _success, false);
            trend.HorizontalAlignment = HorizontalAlignment.Right;
            right.AddChild(trend);
            row.AddChild(right);
            _sidebar.AddChild(card);
        }
    }

    private void BuildBusiness()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        AddSidebarTitle("EMPRESAS", "Abra um negócio e gerencie capital e vagas.", "business");

        if (s.Player.BusinessCompanyId is int companyId)
        {
            var company = s.Companies.FirstOrDefault(c => c.Id == companyId);
            if (company is not null)
            {
                var margin = company.LastRevenue - company.LastCosts;
                _sidebar.AddChild(MakeInfoCard("SUA EMPRESA", company.Name, $"{company.Sector} • {company.Archetype}", _gold));

                var grid = new GridContainer { Columns = 2 };
                grid.AddThemeConstantOverride("h_separation", 7);
                grid.AddThemeConstantOverride("v_separation", 7);
                grid.AddChild(MakeMetricCard("CAIXA", $"Cr$ {company.Cash:N0}", _text));
                grid.AddChild(MakeMetricCard("MARGEM/DIA", $"Cr$ {margin:N0}", margin >= 0 ? _success : _danger));
                grid.AddChild(MakeMetricCard("EQUIPE", $"{company.EmployeeIds.Count}/{company.DesiredEmployees}", _text));
                grid.AddChild(MakeMetricCard("REPUTAÇÃO", company.Reputation.ToString("P0"), _accent));
                _sidebar.AddChild(grid);

                _sidebar.AddChild(MakeLabel(_sim.ExplainCompany(company.Id), 10, _muted));
                _sidebar.AddChild(MakeSection("GESTÃO"));

                var team = new HBoxContainer();
                team.AddThemeConstantOverride("separation", 6);
                var less = MakeButton("− 1 VAGA", false);
                less.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                less.Pressed += () => { _sim.AdjustPlayerBusinessHeadcount(-1); RefreshSidebar(); };
                team.AddChild(less);
                var more = MakeButton("+ 1 VAGA", false);
                more.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                more.Pressed += () => { _sim.AdjustPlayerBusinessHeadcount(1); RefreshSidebar(); };
                team.AddChild(more);
                _sidebar.AddChild(team);

                foreach (var amount in new[] { 500m, 2_000m, 5_000m })
                {
                    var invest = MakeButton($"INVESTIR Cr$ {amount:N0}", amount == 2_000m);
                    var value = amount;
                    invest.Disabled = s.Player.Cash < value;
                    invest.Pressed += () =>
                    {
                        if (_sim.InvestInPlayerBusiness(value))
                        {
                            SetStatus($"Cr$ {value:N0} investidos na empresa.");
                            RefreshAll();
                        }
                    };
                    _sidebar.AddChild(invest);
                }
            }
        }
        else
        {
            _sidebar.AddChild(MakeInfoCard("EMPREENDEDORISMO", "Capital mínimo: Cr$ 5.000",
                "O capital sai do seu patrimônio e entra no caixa real da empresa.", _gold));

            var sector = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
            foreach (var item in ContentCatalog.Sectors) sector.AddItem(item.Name);
            _sidebar.AddChild(sector);

            var open = MakeButton("ABRIR MICROEMPRESA", true, "business");
            open.CustomMinimumSize = new Vector2(0, 50);
            open.Pressed += () =>
            {
                var selected = ContentCatalog.Sectors[sector.Selected].Name;
                SetStatus(_sim.OpenPlayerBusiness(selected) ? $"Empresa aberta em {selected}." : "É preciso ter Cr$ 5.000 e nenhuma empresa.");
                RefreshAll();
            };
            _sidebar.AddChild(open);
        }

        _sidebar.AddChild(MakeSection("MAIORES EMPRESAS"));
        foreach (var company in s.Companies.Where(c => c.Open).OrderByDescending(c => c.Cash).Take(7))
            _sidebar.AddChild(MakeInfoCard(company.Sector.ToUpperInvariant(), company.Name,
                $"Caixa Cr$ {company.Cash:N0} • {company.EmployeeIds.Count} funcionários", company.PlayerOwned ? _gold : _muted));
    }

    private void BuildHistory()
    {
        if (_sim is null || _sidebar is null) return;
        AddSidebarTitle("HISTÓRICO", "A cidade registra acontecimentos e suas causas.", "history");

        foreach (var evt in _sim.State.History.AsEnumerable().Reverse().Take(30))
        {
            var tone = evt.Kind switch
            {
                "Falência" => _danger,
                "Nova empresa" => _success,
                "Contratação" => _accent,
                "Sucessão" => _gold,
                _ => _muted
            };
            _sidebar.AddChild(MakeInfoCard($"DIA {evt.Day} • {evt.Kind.ToUpperInvariant()}", evt.Summary, evt.Cause, tone));
        }
    }

    private void BuildHelp()
    {
        if (_sidebar is null) return;
        AddSidebarTitle("COMO JOGAR", "Seu primeiro ciclo dentro da metrópole.", "help");

        _sidebar.AddChild(MakeGuide("01", "CONSIGA UM EMPREGO", "Abra Carreira, escolha uma vaga e trabalhe para formar patrimônio."));
        _sidebar.AddChild(MakeGuide("02", "CUIDE DO PERSONAGEM", "Energia baixa bloqueia trabalho. Descanse e compre alimentação."));
        _sidebar.AddChild(MakeGuide("03", "LEIA A ECONOMIA", "Oferta, demanda e estoque alteram preços todos os dias."));
        _sidebar.AddChild(MakeGuide("04", "ESCOLHA ONDE MORAR", "Bairros têm custos e vantagens diferentes. Mudar custa dinheiro."));
        _sidebar.AddChild(MakeGuide("05", "ABRA UMA EMPRESA", "Com Cr$ 5.000, escolha um setor e passe a controlar caixa e vagas."));
        _sidebar.AddChild(MakeGuide("06", "ACELERE O TEMPO", "Use 1× a 8× para observar ciclos, falências e novas empresas."));
        _sidebar.AddChild(MakeGuide("07", "SALVE", "Ctrl+S salva. O jogo cria backup antes de substituir o save válido."));
    }

    private void AdvanceOneDay(bool manual)
    {
        if (_sim is null) return;
        try
        {
            _sim.AdvanceHours(24);
            if (_sim.State.CurrentDay % 30 == 0) SaveStore.Save(SavePath, _sim.State);
            if (manual) SetStatus($"Dia {_sim.State.CurrentDay:N0} concluído.");
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
            SetStatus($"Partida salva • dia {_sim.State.CurrentDay:N0}");
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao salvar: {ex.Message}");
            GD.PrintErr(ex);
        }
    }

    private void AddSidebarTitle(string title, string subtitle, string icon)
    {
        if (_sidebar is null) return;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 9);
        row.AddChild(MakeIcon(icon, 28));
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        text.AddChild(MakeLabel(title, 20, _text, false));
        text.AddChild(MakeLabel(subtitle, 10, _muted));
        row.AddChild(text);
        _sidebar.AddChild(row);
        _sidebar.AddChild(MakeDivider());
    }

    private Control MakeMetricCard(string label, string value, Color color)
    {
        var panel = MakePanel(_panel2, 9);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 1);
        panel.AddChild(box);
        box.AddChild(MakeLabel(label, 9, _muted2, false));
        box.AddChild(MakeLabel(value, 16, color));
        return panel;
    }

    private Control MakeProgressStat(string label, decimal value, Color color)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 3);
        var row = new HBoxContainer();
        var left = MakeLabel(label, 10, _muted, false);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(left);
        row.AddChild(MakeLabel($"{value:0}/100", 10, _text, false));
        box.AddChild(row);

        var progress = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = (double)value,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 7)
        };
        var bg = new StyleBoxFlat { BgColor = _panel3, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        var fill = new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        progress.AddThemeStyleboxOverride("background", bg);
        progress.AddThemeStyleboxOverride("fill", fill);
        box.AddChild(progress);
        return box;
    }

    private Control MakeInfoCard(string eyebrow, string title, string detail, Color tone)
    {
        var panel = MakePanel(_panel2, 9);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 4);
        panel.AddChild(box);
        var badge = MakeBadge(eyebrow, tone, tone.Darkened(0.72f));
        badge.HorizontalAlignment = HorizontalAlignment.Left;
        box.AddChild(badge);
        box.AddChild(MakeLabel(title, 13, _text));
        box.AddChild(MakeLabel(detail, 10, _muted));
        return panel;
    }

    private Control MakeGuide(string number, string title, string body)
    {
        var panel = MakePanel(_panel2, 9);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 9);
        panel.AddChild(row);
        var badge = MakeBadge(number, _accent, new Color(0.07f, 0.20f, 0.22f));
        badge.CustomMinimumSize = new Vector2(32, 26);
        row.AddChild(badge);
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 1);
        text.AddChild(MakeLabel(title, 11, _text, false));
        text.AddChild(MakeLabel(body, 10, _muted));
        row.AddChild(text);
        return panel;
    }

    private Label MakeTopStat(HBoxContainer parent, string caption, float width, Color? color = null)
    {
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
        box.AddThemeConstantOverride("separation", 0);
        box.AddChild(MakeLabel(caption, 9, _muted2, false));
        var value = MakeLabel("", 12, color ?? _text, false);
        box.AddChild(value);
        parent.AddChild(box);
        return value;
    }

    private PanelContainer MakePanel(Color color, int radius)
    {
        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = 13,
            ContentMarginRight = 13,
            ContentMarginTop = 11,
            ContentMarginBottom = 11,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = _line
        };
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private Button MakeButton(string text, bool primary, string? icon = null)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 40) };
        button.AddThemeFontSizeOverride("font_size", 11);
        button.AddThemeColorOverride("font_color", _text);
        if (!string.IsNullOrWhiteSpace(icon))
        {
            button.Icon = GD.Load<Texture2D>($"res://assets/ui/{icon}.svg");
            button.IconAlignment = HorizontalAlignment.Left;
        }
        ApplyButtonStyle(button, primary, false);
        button.MouseEntered += () => _audio?.PlayHover();
        button.Pressed += () => _audio?.PlayClick();
        return button;
    }

    private void ApplyButtonStyle(Button button, bool primary, bool selected)
    {
        var normalBg = primary ? new Color(0.08f, 0.45f, 0.48f) : selected ? new Color(0.07f, 0.23f, 0.26f) : _panel2;
        var normalBorder = primary || selected ? _accent : _line;
        button.AddThemeStyleboxOverride("normal", ButtonStyle(normalBg, normalBorder));
        button.AddThemeStyleboxOverride("hover", ButtonStyle(primary ? new Color(0.10f, 0.52f, 0.55f) : _panel3, primary || selected ? _accent : new Color(0.20f, 0.33f, 0.43f)));
        button.AddThemeStyleboxOverride("pressed", ButtonStyle(new Color(0.05f, 0.31f, 0.35f), _accent));
        button.AddThemeStyleboxOverride("focus", ButtonStyle(normalBg, _accent));
        button.AddThemeStyleboxOverride("disabled", ButtonStyle(new Color(0.05f, 0.07f, 0.09f), new Color(0.10f, 0.14f, 0.18f)));
    }

    private StyleBoxFlat ButtonStyle(Color bg, Color border) => new()
    {
        BgColor = bg,
        CornerRadiusTopLeft = 8,
        CornerRadiusTopRight = 8,
        CornerRadiusBottomLeft = 8,
        CornerRadiusBottomRight = 8,
        ContentMarginLeft = 10,
        ContentMarginRight = 10,
        ContentMarginTop = 7,
        ContentMarginBottom = 7,
        BorderWidthLeft = 1,
        BorderWidthTop = 1,
        BorderWidthRight = 1,
        BorderWidthBottom = 1,
        BorderColor = border
    };

    private LineEdit MakeLineEdit(string placeholder, string text)
    {
        var edit = new LineEdit { PlaceholderText = placeholder, Text = text, CustomMinimumSize = new Vector2(0, 46) };
        edit.AddThemeFontSizeOverride("font_size", 12);
        edit.AddThemeColorOverride("font_color", _text);
        edit.AddThemeColorOverride("font_placeholder_color", _muted2);
        var style = ButtonStyle(_panel2, _line);
        edit.AddThemeStyleboxOverride("normal", style);
        edit.AddThemeStyleboxOverride("focus", ButtonStyle(_panel2, _accent));
        return edit;
    }

    private Label MakeLabel(string text, int fontSize, Color color, bool wrap = true)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
            Modulate = color
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private Label MakeBadge(string text, Color fg, Color bg)
    {
        var label = MakeLabel(text, 9, fg, false);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = new Vector2(0, 23);
        label.AddThemeStyleboxOverride("normal", new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 7,
            ContentMarginRight = 7,
            ContentMarginTop = 3,
            ContentMarginBottom = 3
        });
        return label;
    }

    private TextureRect MakeIcon(string name, int size) => new()
    {
        Texture = GD.Load<Texture2D>($"res://assets/ui/{name}.svg"),
        CustomMinimumSize = new Vector2(size, size),
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private Control MakeLegend(Color color, string text)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        row.AddChild(new ColorRect { Color = color, CustomMinimumSize = new Vector2(8, 8), MouseFilter = MouseFilterEnum.Ignore });
        row.AddChild(MakeLabel(text, 9, _muted, false));
        return row;
    }

    private Label MakeSection(string text)
    {
        var label = MakeLabel(text, 9, _muted2, false);
        label.CustomMinimumSize = new Vector2(0, 22);
        label.VerticalAlignment = VerticalAlignment.Bottom;
        return label;
    }

    private HSeparator MakeDivider()
    {
        var sep = new HSeparator { CustomMinimumSize = new Vector2(0, 1) };
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat { BgColor = _line });
        return sep;
    }

    private VSeparator MakeVSeparator()
    {
        var sep = new VSeparator { CustomMinimumSize = new Vector2(1, 0) };
        sep.AddThemeStyleboxOverride("separator", new StyleBoxFlat { BgColor = _line });
        return sep;
    }

    private static Control MakeSpacer(float height) => new Control { CustomMinimumSize = new Vector2(0, height) };

    private void AddBackground()
    {
        var bg = new ColorRect { Color = _bg, MouseFilter = MouseFilterEnum.Ignore };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
    }

    private void SetStatus(string text)
    {
        if (_statusLabel is not null) _statusLabel.Text = text;
    }

    private static void ClearNode(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void AdvanceSimulationHour()
    {
        if (_sim is null) return;

        try
        {
            var previousDay = _sim.State.CurrentDay;
            _sim.AdvanceHours(1);

            if (_sim.State.CurrentDay != previousDay && _sim.State.CurrentDay % 30 == 0)
                SaveStore.Save(SavePath, _sim.State);

            RefreshAll();
        }
        catch (Exception ex)
        {
            _speed = 0;
            SetStatus($"Simulação pausada por erro: {ex.Message}");
            GD.PrintErr(ex);
        }
    }

    private void BuildLife()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;
        var p = s.Player;
        var district = s.Districts.First(d => d.Id == p.DistrictId);

        AddSidebarTitle("VIDA", "Seu tempo, corpo, mente e trajetória pessoal.", "life");

        _sidebar.AddChild(MakeInfoCard(
            $"{s.CurrentHour:00}:00 • {s.Weather.ToUpperInvariant()}",
            p.CurrentActivity,
            $"{district.Name} • {s.TemperatureC:0}°C • {p.AgeYears} anos • geração {p.Generation}",
            _accent));

        var needs = MakePanel(_panel2, 10);
        var nbox = new VBoxContainer();
        nbox.AddThemeConstantOverride("separation", 7);
        needs.AddChild(nbox);
        nbox.AddChild(MakeLabel("NECESSIDADES E BEM-ESTAR", 10, _muted2, false));
        nbox.AddChild(MakeProgressStat("Energia", p.Energy, p.Energy < 25 ? _danger : _accent));
        nbox.AddChild(MakeProgressStat("Fome", p.Hunger, p.Hunger > 75 ? _danger : _gold));
        nbox.AddChild(MakeProgressStat("Saúde", p.Health, p.Health < 40 ? _danger : _success));
        nbox.AddChild(MakeProgressStat("Estresse", p.Stress, p.Stress > 70 ? _danger : _gold));
        nbox.AddChild(MakeProgressStat("Felicidade", p.Happiness, p.Happiness < 35 ? _danger : _success));
        nbox.AddChild(MakeProgressStat("Vida social", p.Social, p.Social < 30 ? _gold : _accent));
        nbox.AddChild(MakeProgressStat("Condicionamento", p.Fitness, _success));
        _sidebar.AddChild(needs);

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 7);
        grid.AddThemeConstantOverride("v_separation", 7);
        grid.AddChild(MakeMetricCard("EDUCAÇÃO", EducationName(p.EducationLevel), _text));
        grid.AddChild(MakeMetricCard("CARREIRA", $"{p.CareerExperienceDays:N0} dias", _accent));
        grid.AddChild(MakeMetricCard("REPUTAÇÃO", p.CareerReputation.ToString("P0"), _gold));
        grid.AddChild(MakeMetricCard("RELAÇÃO", p.RelationshipStatus, _text));
        _sidebar.AddChild(grid);

        if (!string.IsNullOrWhiteSpace(p.PartnerName))
            _sidebar.AddChild(MakeInfoCard("RELACIONAMENTO", p.PartnerName!, $"{p.Children} filho(s) • vida social {p.Social:0}/100", _accent));

        _sidebar.AddChild(MakeSection("AÇÕES DE VIDA"));

        if (p.EmployerCompanyId is not null)
        {
            var work = MakeButton("TRABALHAR 8H", true, "career");
            work.Disabled = p.Energy < 18m;
            work.Pressed += () =>
            {
                _sim.WorkShift(8);
                SetStatus("Você concluiu um turno de trabalho.");
                RefreshAll();
            };
            _sidebar.AddChild(work);
        }

        var sleep = MakeButton("DORMIR 8H", false);
        sleep.Pressed += () =>
        {
            _sim.Sleep(8);
            SetStatus("Sono recuperado por 8 horas.");
            RefreshAll();
        };
        _sidebar.AddChild(sleep);

        var study = MakeButton($"ESTUDAR 4H • Cr$ {4 * (8 + p.EducationLevel * 3):N0}", false);
        study.Pressed += () =>
        {
            SetStatus(_sim.Study(4) ? "Sessão de estudo concluída." : "Caixa insuficiente para estudar.");
            RefreshAll();
        };
        _sidebar.AddChild(study);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        var social = MakeButton("SOCIALIZAR 3H", false);
        social.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        social.Pressed += () =>
        {
            _sim.Socialize(3);
            SetStatus("Você dedicou tempo à vida social.");
            RefreshAll();
        };
        actionRow.AddChild(social);

        var exercise = MakeButton("EXERCÍCIO 2H", false);
        exercise.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        exercise.Pressed += () =>
        {
            _sim.Exercise(2);
            SetStatus("Treino concluído.");
            RefreshAll();
        };
        actionRow.AddChild(exercise);
        _sidebar.AddChild(actionRow);

        _sidebar.AddChild(MakeSection("CONTEXTO DE VIDA"));
        var job = p.EmployerCompanyId is int employerId
            ? s.Companies.FirstOrDefault(c => c.Id == employerId)
            : null;
        _sidebar.AddChild(MakeInfoCard(
            "TRABALHO",
            job?.Name ?? "Sem emprego",
            job is null
                ? "Procure oportunidades em Carreira."
                : $"{job.Sector} • Cr$ {p.DailyWage:N2}/dia • {p.WorkedHoursToday:0.#}/8h hoje",
            job is null ? _gold : _accent));

        _sidebar.AddChild(MakeInfoCard(
            "MORADIA",
            district.Name,
            $"Aluguel {district.RentIndex:0.00}× • vida social {district.SocialIndex:0.00}× • segurança {district.SafetyIndex:0.00}×",
            _text));
    }

    private void BuildPeople()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;

        AddSidebarTitle("PESSOAS", "A cidade é feita de indivíduos com rotina, ambição e história.", "people");

        var alive = s.Citizens.Where(c => c.Alive).ToArray();
        var working = alive.Count(c => c.CurrentActivity == "Trabalhando");
        var studying = alive.Count(c => c.CurrentActivity == "Estudando");
        var sleeping = alive.Count(c => c.CurrentActivity == "Dormindo");
        var leisure = alive.Count(c => c.CurrentActivity == "Lazer");

        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 7);
        grid.AddThemeConstantOverride("v_separation", 7);
        grid.AddChild(MakeMetricCard("TRABALHANDO", working.ToString("N0"), _accent));
        grid.AddChild(MakeMetricCard("ESTUDANDO", studying.ToString("N0"), _gold));
        grid.AddChild(MakeMetricCard("DORMINDO", sleeping.ToString("N0"), _muted));
        grid.AddChild(MakeMetricCard("EM LAZER", leisure.ToString("N0"), _success));
        _sidebar.AddChild(grid);

        var avgHappiness = alive.Length == 0 ? 0m : alive.Average(c => c.Happiness);
        var avgStress = alive.Length == 0 ? 0m : alive.Average(c => c.Stress);
        _sidebar.AddChild(MakeProgressStat("Felicidade média", avgHappiness, avgHappiness < 40 ? _danger : _success));
        _sidebar.AddChild(MakeProgressStat("Estresse médio", avgStress, avgStress > 65 ? _danger : _gold));

        _sidebar.AddChild(MakeSection("VIDAS EM DESTAQUE"));

        var featured = alive
            .OrderByDescending(c => PersonInterestScore(c, s.CurrentDay))
            .Take(12)
            .ToArray();

        foreach (var citizen in featured)
        {
            var employer = citizen.EmployedCompanyId is int companyId
                ? s.Companies.FirstOrDefault(c => c.Id == companyId)
                : null;
            var partner = citizen.PartnerCitizenId is int partnerId
                ? s.Citizens.FirstOrDefault(c => c.Id == partnerId)
                : null;

            var card = MakePanel(_panel2, 9);
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 4);
            card.AddChild(box);

            var top = new HBoxContainer();
            var name = MakeLabel(citizen.Name, 13, _text, true);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            top.AddChild(name);
            top.AddChild(MakeBadge(citizen.CurrentActivity.ToUpperInvariant(), ActivityColor(citizen.CurrentActivity), _panel3));
            box.AddChild(top);

            box.AddChild(MakeLabel(
                $"{citizen.AgeYears} anos • {EducationName(citizen.EducationLevel)} • {PersonalityName(citizen)}",
                10, _muted));

            var lifeLine = employer is null
                ? "Sem emprego"
                : $"{CompanyDisplayName(employer)} • Cr$ {citizen.DailyWage:N0}/dia";
            if (partner is not null)
                lifeLine += $" • parceiro(a): {partner.Name.Split(' ')[0]}";
            box.AddChild(MakeLabel(lifeLine, 10, _muted));

            var stats = new HBoxContainer();
            var happy = MakeLabel($"☺ {citizen.Happiness:0}", 10, citizen.Happiness > 60 ? _success : _gold, false);
            happy.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            stats.AddChild(happy);
            stats.AddChild(MakeLabel($"estresse {citizen.Stress:0}", 10, citizen.Stress > 65 ? _danger : _muted, false));
            stats.AddChild(MakeLabel($"Cr$ {citizen.Cash:N0}", 10, _text, false));
            box.AddChild(stats);

            _sidebar.AddChild(card);
        }

        _sidebar.AddChild(MakeSection("ÚLTIMAS HISTÓRIAS HUMANAS"));
        foreach (var evt in s.History
                     .Where(e => e.Kind is "Carreira" or "Relacionamento" or "Nascimento" or "Falecimento" or "Educação")
                     .AsEnumerable().Reverse().Take(6))
        {
            _sidebar.AddChild(MakeInfoCard($"DIA {evt.Day} • {evt.Kind.ToUpperInvariant()}", evt.Summary, evt.Cause, _muted));
        }
    }

    private void BuildBusinessDeep()
    {
        if (_sim is null || _sidebar is null) return;
        var s = _sim.State;

        AddSidebarTitle("EMPRESAS", "Marca, finanças, pessoas, produto e concorrência em uma única operação.", "business");

        if (s.Player.BusinessCompanyId is not int companyId)
        {
            BuildBusinessFounding();
            return;
        }

        var company = s.Companies.FirstOrDefault(c => c.Id == companyId && c.Open && c.PlayerOwned);
        if (company is null)
        {
            _sidebar.AddChild(MakeInfoCard("SEM EMPRESA ATIVA", "Sua antiga empresa não está mais operando.",
                "Abra uma nova empresa quando tiver capital. O histórico da cidade preserva o que aconteceu.", _danger));
            BuildBusinessFounding();
            return;
        }

        var profit = company.LastRevenue - company.LastCosts;
        var rival = company.RivalCompanyId is int rivalId
            ? s.Companies.FirstOrDefault(c => c.Id == rivalId && c.Open)
            : null;

        var brandCard = MakePanel(_panel2, 11);
        var brandBox = new VBoxContainer();
        brandBox.AddThemeConstantOverride("separation", 5);
        brandCard.AddChild(brandBox);

        var brandTop = new HBoxContainer();
        var brandName = MakeLabel(CompanyDisplayName(company), 20, _text, true);
        brandName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        brandTop.AddChild(brandName);
        brandTop.AddChild(MakeBadge(company.OperatingStatus.ToUpperInvariant(),
            company.OperatingStatus == "Ativa" ? _success : company.OperatingStatus == "Atenção" ? _gold : _danger,
            _panel3));
        brandBox.AddChild(brandTop);
        brandBox.AddChild(MakeLabel(company.Slogan, 11, _muted));
        brandBox.AddChild(MakeLabel($"{company.Sector} • {company.Archetype} • estratégia {company.Strategy}", 10, _muted2));
        _sidebar.AddChild(brandCard);

        var metrics = new GridContainer { Columns = 2 };
        metrics.AddThemeConstantOverride("h_separation", 7);
        metrics.AddThemeConstantOverride("v_separation", 7);
        metrics.AddChild(MakeMetricCard("CAIXA", $"Cr$ {company.Cash:N0}", _text));
        metrics.AddChild(MakeMetricCard("LUCRO/DIA", $"Cr$ {profit:N0}", profit >= 0 ? _success : _danger));
        metrics.AddChild(MakeMetricCard("DÍVIDA", $"Cr$ {company.Debt:N0}", company.Debt > company.Cash ? _danger : _gold));
        metrics.AddChild(MakeMetricCard("MARKET SHARE", company.MarketShare.ToString("P1"), _accent));
        _sidebar.AddChild(metrics);

        var chart = new FinanceChart
        {
            CustomMinimumSize = new Vector2(0, 145),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        chart.SetCompany(company);
        _sidebar.AddChild(chart);

        _sidebar.AddChild(MakeSection("DRE DO ÚLTIMO DIA"));
        var dre = MakePanel(_panel2, 9);
        var dbox = new VBoxContainer();
        dbox.AddThemeConstantOverride("separation", 4);
        dre.AddChild(dbox);
        AddFinanceRow(dbox, "Receita", company.LastRevenue, _success);
        AddFinanceRow(dbox, "Folha", -company.LastPayroll, _text);
        AddFinanceRow(dbox, "Operação", -company.LastOperations, _text);
        AddFinanceRow(dbox, "Aluguel", -company.LastRent, _text);
        AddFinanceRow(dbox, "Marketing", -company.LastMarketing, _text);
        AddFinanceRow(dbox, "Impostos", -company.LastTaxes, _text);
        dbox.AddChild(MakeDivider());
        AddFinanceRow(dbox, "Resultado", profit, profit >= 0 ? _success : _danger);
        _sidebar.AddChild(dre);

        _sidebar.AddChild(MakeSection("MARCA E POSICIONAMENTO"));
        var brandInput = MakeLineEdit("Nome da marca", CompanyDisplayName(company));
        _sidebar.AddChild(brandInput);
        var sloganInput = MakeLineEdit("Slogan", company.Slogan);
        _sidebar.AddChild(sloganInput);
        var applyBrand = MakeButton("APLICAR BRANDING • Cr$ 300", false);
        applyBrand.Pressed += () =>
        {
            SetStatus(_sim.ConfigureBrand(brandInput.Text, sloganInput.Text)
                ? "Branding atualizado."
                : "Não foi possível atualizar a marca.");
            RefreshAll();
        };
        _sidebar.AddChild(applyBrand);

        var brandGrid = new GridContainer { Columns = 2 };
        brandGrid.AddThemeConstantOverride("h_separation", 7);
        brandGrid.AddThemeConstantOverride("v_separation", 7);
        brandGrid.AddChild(MakeMetricCard("CONHECIMENTO", company.BrandAwareness.ToString("P0"), _accent));
        brandGrid.AddChild(MakeMetricCard("FIDELIDADE", company.CustomerLoyalty.ToString("P0"), _gold));
        brandGrid.AddChild(MakeMetricCard("QUALIDADE", company.ProductQuality.ToString("P0"), _success));
        brandGrid.AddChild(MakeMetricCard("INOVAÇÃO", company.Innovation.ToString("P0"), _accent));
        _sidebar.AddChild(brandGrid);

        _sidebar.AddChild(MakeSection("PREÇO E MARKETING"));
        var strategy = new OptionButton { CustomMinimumSize = new Vector2(0, 42) };
        foreach (var item in new[] { "Equilibrada", "Penetração", "Premium", "Crescimento", "Eficiência" })
            strategy.AddItem(item);
        var currentStrategy = Math.Max(0, Array.IndexOf(new[] { "Equilibrada", "Penetração", "Premium", "Crescimento", "Eficiência" }, company.Strategy));
        strategy.Select(currentStrategy);
        _sidebar.AddChild(strategy);

        var setStrategy = MakeButton($"APLICAR ESTRATÉGIA • preço {company.PriceMultiplier:0.00}×", true);
        setStrategy.Pressed += () =>
        {
            _sim.SetPricingStrategy(strategy.GetItemText(strategy.Selected));
            SetStatus("Estratégia comercial atualizada.");
            RefreshAll();
        };
        _sidebar.AddChild(setStrategy);

        var marketingRow = new HBoxContainer();
        marketingRow.AddThemeConstantOverride("separation", 6);
        var mLess = MakeButton("− Cr$ 10/dia", false);
        mLess.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mLess.Pressed += () => { _sim.AdjustMarketingBudget(-10m); RefreshAll(); };
        marketingRow.AddChild(mLess);
        var mNow = MakeButton($"MARKETING Cr$ {company.MarketingBudgetDaily:N0}/dia", false);
        mNow.Disabled = true;
        mNow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        marketingRow.AddChild(mNow);
        var mMore = MakeButton("+ Cr$ 10/dia", false);
        mMore.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mMore.Pressed += () => { _sim.AdjustMarketingBudget(10m); RefreshAll(); };
        marketingRow.AddChild(mMore);
        _sidebar.AddChild(marketingRow);

        _sidebar.AddChild(MakeSection("PRODUTO E INOVAÇÃO"));
        _sidebar.AddChild(MakeProgressStat("Qualidade", company.ProductQuality * 100m, _success));
        _sidebar.AddChild(MakeProgressStat("Inovação", company.Innovation * 100m, _accent));
        _sidebar.AddChild(MakeProgressStat("Moral da equipe", company.EmployeeMorale * 100m, company.EmployeeMorale < 0.4m ? _danger : _gold));

        var productRow = new HBoxContainer();
        productRow.AddThemeConstantOverride("separation", 6);
        var quality = MakeButton("QUALIDADE • Cr$ 1.500", false);
        quality.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        quality.Pressed += () =>
        {
            SetStatus(_sim.InvestInQuality(1_500m) ? "Investimento em qualidade concluído." : "Caixa da empresa insuficiente.");
            RefreshAll();
        };
        productRow.AddChild(quality);
        var innovation = MakeButton("P&D • Cr$ 1.500", false);
        innovation.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        innovation.Pressed += () =>
        {
            SetStatus(_sim.InvestInInnovation(1_500m) ? "Investimento em P&D concluído." : "Caixa da empresa insuficiente.");
            RefreshAll();
        };
        productRow.AddChild(innovation);
        _sidebar.AddChild(productRow);

        _sidebar.AddChild(MakeSection("PESSOAS E RH"));
        var hr = MakePanel(_panel2, 9);
        var hbox = new VBoxContainer();
        hbox.AddThemeConstantOverride("separation", 5);
        hr.AddChild(hbox);
        hbox.AddChild(MakeLabel($"{company.EmployeeIds.Count}/{company.DesiredEmployees} funcionários • salário-base Cr$ {company.BaseWage:N2}/dia", 11, _text));
        hbox.AddChild(MakeLabel($"Moral {company.EmployeeMorale:P0} • produtividade {company.Productivity:0.00}×", 10, _muted));

        var wageRow = new HBoxContainer();
        wageRow.AddThemeConstantOverride("separation", 6);
        var wageDown = MakeButton("SALÁRIO −5%", false);
        wageDown.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        wageDown.Pressed += () => { _sim.AdjustBaseWage(-0.05m); RefreshAll(); };
        wageRow.AddChild(wageDown);
        var wageUp = MakeButton("SALÁRIO +5%", false);
        wageUp.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        wageUp.Pressed += () => { _sim.AdjustBaseWage(0.05m); RefreshAll(); };
        wageRow.AddChild(wageUp);
        hbox.AddChild(wageRow);

        var teamRow = new HBoxContainer();
        teamRow.AddThemeConstantOverride("separation", 6);
        var less = MakeButton("− 1 VAGA", false);
        less.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        less.Pressed += () => { _sim.AdjustPlayerBusinessHeadcount(-1); RefreshAll(); };
        teamRow.AddChild(less);
        var more = MakeButton("+ 1 VAGA", false);
        more.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        more.Pressed += () => { _sim.AdjustPlayerBusinessHeadcount(1); RefreshAll(); };
        teamRow.AddChild(more);
        hbox.AddChild(teamRow);
        _sidebar.AddChild(hr);

        _sidebar.AddChild(MakeSection("CONCORRÊNCIA E RIVALIDADE"));
        if (rival is null)
        {
            _sidebar.AddChild(MakeInfoCard("SEM RIVAL DIRETO", "Mercado pulverizado",
                "Nenhum concorrente direto concentra participação suficiente agora.", _muted));
        }
        else
        {
            var rivalryTone = company.RivalryIntensity > 0.70m ? _danger : company.RivalryIntensity > 0.45m ? _gold : _accent;
            _sidebar.AddChild(MakeInfoCard(
                $"RIVALIDADE {company.RivalryIntensity:P0}",
                CompanyDisplayName(rival),
                $"{rival.Strategy} • share {rival.MarketShare:P1} • marca {rival.BrandAwareness:P0} • qualidade {rival.ProductQuality:P0}",
                rivalryTone));
        }

        _sidebar.AddChild(MakeSection("APORTE DO SÓCIO"));
        var investRow = new HBoxContainer();
        investRow.AddThemeConstantOverride("separation", 6);
        foreach (var amount in new[] { 500m, 2_000m, 5_000m })
        {
            var button = MakeButton($"Cr$ {amount:N0}", amount == 2_000m);
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var value = amount;
            button.Disabled = s.Player.Cash < value;
            button.Pressed += () =>
            {
                if (_sim.InvestInPlayerBusiness(value))
                {
                    SetStatus($"Aporte de Cr$ {value:N0} realizado.");
                    RefreshAll();
                }
            };
            investRow.AddChild(button);
        }
        _sidebar.AddChild(investRow);
    }

    private void BuildBusinessFounding()
    {
        if (_sim is null || _sidebar is null) return;

        _sidebar.AddChild(MakeInfoCard(
            "NOVO NEGÓCIO",
            "Capital do fundador: Cr$ 5.000",
            "O jogo adiciona uma pequena linha de crédito inicial. Depois disso, caixa, dívida, marca e concorrência passam a ser simulados.",
            _gold));

        var sector = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
        foreach (var item in ContentCatalog.Sectors) sector.AddItem(item.Name);
        _sidebar.AddChild(sector);

        var strategy = new OptionButton { CustomMinimumSize = new Vector2(0, 44) };
        foreach (var item in new[] { "Equilibrada", "Penetração", "Premium", "Crescimento", "Eficiência" })
            strategy.AddItem(item);
        _sidebar.AddChild(strategy);

        var open = MakeButton("FUNDAR EMPRESA", true, "business");
        open.CustomMinimumSize = new Vector2(0, 52);
        open.Pressed += () =>
        {
            var selectedSector = ContentCatalog.Sectors[sector.Selected].Name;
            if (_sim.OpenPlayerBusiness(selectedSector))
            {
                _sim.SetPricingStrategy(strategy.GetItemText(strategy.Selected));
                SetStatus($"Empresa fundada no setor {selectedSector}.");
            }
            else
            {
                SetStatus("É preciso ter Cr$ 5.000 e nenhuma empresa ativa.");
            }
            RefreshAll();
        };
        _sidebar.AddChild(open);

        _sidebar.AddChild(MakeSection("O QUE SERÁ SIMULADO"));
        foreach (var text in new[]
                 {
                     "Marca, awareness, fidelidade e reputação",
                     "Preço, qualidade, inovação e marketing",
                     "Folha, aluguel, operação, impostos, caixa e dívida",
                     "Contratações, moral, salários e produtividade",
                     "Participação de mercado, concorrentes e rivalidade",
                     "Crise, crédito, recuperação, insolvência e falência"
                 })
            _sidebar.AddChild(MakeLabel($"• {text}", 10, _muted));
    }

    private void AddFinanceRow(VBoxContainer box, string label, decimal value, Color color)
    {
        var row = new HBoxContainer();
        var left = MakeLabel(label, 10, _muted, false);
        left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(left);
        var right = MakeLabel($"{(value < 0 ? "−" : "")}Cr$ {Math.Abs(value):N2}", 10, color, false);
        right.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(right);
        box.AddChild(row);
    }

    private static string EducationName(int level) => level switch
    {
        <= 0 => "Sem escolaridade",
        1 => "Básica",
        2 => "Média",
        3 => "Superior",
        4 => "Especialização",
        _ => "Avançada"
    };

    private static string PersonalityName(CitizenState citizen)
    {
        var max = Math.Max(Math.Max(citizen.Ambition, citizen.Sociability), Math.Max(citizen.Discipline, citizen.RiskTolerance));
        if (max == citizen.Ambition) return "Ambicioso";
        if (max == citizen.Sociability) return "Sociável";
        if (max == citizen.Discipline) return "Disciplinado";
        return "Ousado";
    }

    private static decimal PersonInterestScore(CitizenState citizen, int day)
    {
        var activity = citizen.CurrentActivity is "Trabalhando" or "Estudando" ? 8m : 0m;
        var extremes = Math.Abs(citizen.Happiness - 50m) + Math.Abs(citizen.Stress - 50m);
        return extremes + activity + citizen.Ambition * 10m + ((citizen.Id * 17 + day) % 19);
    }

    private Color ActivityColor(string activity) => activity switch
    {
        "Trabalhando" => _accent,
        "Estudando" => _gold,
        "Dormindo" => _muted,
        "Lazer" => _success,
        _ => _text
    };

    private static string CompanyDisplayName(CompanyState company) =>
        string.IsNullOrWhiteSpace(company.BrandName) ? company.Name : company.BrandName;

}
