namespace Metropole.Sim;

public static class PlayerActions
{
    public static bool LeaveJob(this SimulationEngine engine)
    {
        var state = engine.State;
        if (state.Player.EmployerCompanyId is not int companyId) return false;

        var company = state.Companies.FirstOrDefault(c => c.Id == companyId);
        state.Player.EmployerCompanyId = null;
        state.Player.DailyWage = 0m;
        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Carreira",
            Summary = $"{state.Player.Name} deixou a {company?.Name ?? "empresa atual"}.",
            Cause = "decisão do jogador → desligamento voluntário",
            EntityId = companyId
        });
        return true;
    }

    public static int BuyFood(this SimulationEngine engine, int units)
    {
        if (units <= 0) return 0;

        var state = engine.State;
        var market = state.Markets.First(m => m.Family == "Alimentos");
        var sellers = state.Companies
            .Where(c => c.Open && c.ProductFamily == market.Family)
            .OrderByDescending(c => c.Reputation)
            .ToArray();

        if (sellers.Length == 0) return 0;

        var bought = 0;
        for (var i = 0; i < units; i++)
        {
            if (market.Stock < 1m || state.Player.Cash < market.Price) break;

            var seller = sellers[i % sellers.Length];
            state.Player.Cash -= market.Price;
            seller.Cash += market.Price;
            seller.LastRevenue += market.Price;
            market.Stock -= 1m;
            market.DailyDemand += 1m;
            state.Player.Hunger = Math.Clamp(state.Player.Hunger - 40m, 0m, 100m);
            bought++;
        }

        if (bought > 0)
        {
            state.History.Add(new HistoryEvent
            {
                Day = state.CurrentDay,
                Kind = "Consumo",
                Summary = $"{state.Player.Name} comprou {bought} unidade(s) de alimentação.",
                Cause = "necessidade → compra → receita distribuída no mercado de alimentos"
            });
        }
        return bought;
    }

    public static decimal GetMoveCost(this SimulationEngine engine, int districtId)
    {
        var district = engine.State.Districts.FirstOrDefault(d => d.Id == districtId)
            ?? throw new ArgumentOutOfRangeException(nameof(districtId));
        return decimal.Round(220m + 260m * district.RentIndex + 90m * district.WealthIndex, 2);
    }

    public static bool MovePlayerDistrict(this SimulationEngine engine, int districtId)
    {
        var state = engine.State;
        if (districtId == state.Player.DistrictId) return true;

        var district = state.Districts.FirstOrDefault(d => d.Id == districtId);
        if (district is null) return false;

        var cost = engine.GetMoveCost(districtId);
        if (state.Player.Cash < cost) return false;

        var previous = state.Districts.First(d => d.Id == state.Player.DistrictId);
        state.Player.Cash -= cost;
        state.Treasury += cost;
        state.Player.DistrictId = district.Id;

        if (state.Player.PartnerCitizenId is int partnerId)
        {
            var partner = state.Citizens.FirstOrDefault(c => c.Id == partnerId && c.Alive && c.IsPlayerPartner);
            if (partner is not null)
                partner.DistrictId = district.Id;
        }

        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Moradia",
            Summary = $"{state.Player.Name} mudou de {previous.Name} para {district.Name}.",
            Cause = $"custo Cr$ {cost:0.00} → novo índice de aluguel {district.RentIndex:0.00}×",
            EntityId = district.Id
        });
        return true;
    }

    public static bool InvestInPlayerBusiness(this SimulationEngine engine, decimal amount)
    {
        var state = engine.State;
        if (amount <= 0m || state.Player.Cash < amount || state.Player.BusinessCompanyId is not int companyId)
            return false;

        var company = state.Companies.FirstOrDefault(c => c.Id == companyId && c.Open && c.PlayerOwned);
        if (company is null) return false;

        state.Player.Cash -= amount;
        company.Cash += amount;
        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Empresa",
            Summary = $"{state.Player.Name} aportou Cr$ {amount:0.00} em {company.Name}.",
            Cause = "patrimônio pessoal → capital de giro",
            EntityId = company.Id
        });
        return true;
    }

    public static bool AdjustPlayerBusinessHeadcount(this SimulationEngine engine, int delta)
    {
        var state = engine.State;
        if (delta == 0 || state.Player.BusinessCompanyId is not int companyId) return false;

        var company = state.Companies.FirstOrDefault(c => c.Id == companyId && c.Open && c.PlayerOwned);
        if (company is null) return false;

        var next = Math.Clamp(company.DesiredEmployees + delta, 1, 50);
        if (next == company.DesiredEmployees) return false;

        company.DesiredEmployees = next;
        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = "Empresa",
            Summary = $"{company.Name} ajustou a meta de equipe para {next}.",
            Cause = delta > 0 ? "expansão planejada → nova vaga" : "redução planejada → meta de quadro menor",
            EntityId = company.Id
        });
        return true;
    }
}
