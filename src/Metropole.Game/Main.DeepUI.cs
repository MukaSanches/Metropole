using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class Main
{
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
                : $"{employer.BrandNameOrName()} • Cr$ {citizen.DailyWage:N0}/dia";
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
        var brandName = MakeLabel(company.BrandNameOrName(), 20, _text, true);
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
        var brandInput = MakeLineEdit("Nome da marca", company.BrandNameOrName());
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
                rival.BrandNameOrName(),
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
}

internal static class CompanyDisplayExtensions
{
    public static string BrandNameOrName(this CompanyState company) =>
        string.IsNullOrWhiteSpace(company.BrandName) ? company.Name : company.BrandName;
}
