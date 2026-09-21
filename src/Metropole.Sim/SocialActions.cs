namespace Metropole.Sim;

public static class SocialActions
{
    public static void UpdateDailyPlayerRelationships(GameState state)
    {
        var player = state.Player;

        if (player.PartnerCitizenId is int partnerId)
        {
            var partner = state.Citizens.FirstOrDefault(c => c.Id == partnerId && c.Alive && c.IsPlayerPartner);
            if (partner is null)
            {
                var oldName = player.PartnerName ?? "parceiro";
                player.PartnerCitizenId = null;
                player.PartnerName = oldName;
                player.RelationshipStatus = "Viúvo";
                player.RelationshipStartDay = -1;
                player.MarriageDay = -1;
                player.Happiness = Math.Clamp(player.Happiness - 16m, 0m, 100m);
                player.Stress = Math.Clamp(player.Stress + 18m, 0m, 100m);

                AddHistory(state, "Relacionamento", $"{player.Name} perdeu {oldName}.",
                    "falecimento do parceiro → viuvez e impacto emocional");
            }
            else
            {
                var daysWithoutInteraction = Math.Max(0, state.CurrentDay - partner.LastPlayerInteractionDay);

                if (daysWithoutInteraction >= 4)
                {
                    var neglect = Math.Min(1.4m, 0.10m + (daysWithoutInteraction - 3) * 0.035m);
                    partner.PlayerAffinity = Math.Clamp(partner.PlayerAffinity - neglect, 0m, 100m);
                    partner.PlayerTrust = Math.Clamp(partner.PlayerTrust - neglect * 0.42m, 0m, 100m);
                    player.Social = Math.Clamp(player.Social - 0.18m, 0m, 100m);
                }
                else if (partner.DistrictId == player.DistrictId)
                {
                    player.Social = Math.Clamp(player.Social + 0.08m, 0m, 100m);
                    player.Happiness = Math.Clamp(player.Happiness + 0.04m, 0m, 100m);
                }
            }
        }

        foreach (var citizen in state.Citizens.Where(c =>
                     c.Alive &&
                     !c.IsPlayerPartner &&
                     c.PlayerFamiliarity > 0m))
        {
            var gap = Math.Max(0, state.CurrentDay - citizen.LastPlayerInteractionDay);
            if (gap < 14) continue;

            var familiarityDecay = Math.Min(0.16m, 0.02m + (gap - 14) * 0.002m);
            var affinityDecay = Math.Min(0.12m, 0.015m + (gap - 14) * 0.0015m);

            citizen.PlayerFamiliarity = Math.Clamp(citizen.PlayerFamiliarity - familiarityDecay, 0m, 100m);
            citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity - affinityDecay, 0m, 100m);

            if (citizen.PlayerFamiliarity < 8m)
                citizen.PlayerRelationshipStatus = "Conhecido distante";
        }
    }

    public static IReadOnlyList<CitizenState> GetNearbyPeople(this SimulationEngine engine, int limit = 24)
    {
        var state = engine.State;
        return state.Citizens
            .Where(c => c.Alive
                        && c.AgeYears >= 18
                        && c.DistrictId == state.Player.DistrictId
                        && c.CurrentActivity != "Dormindo")
            .OrderByDescending(c => c.IsPlayerPartner)
            .ThenByDescending(c => c.PlayerAffinity + c.PlayerTrust + c.PlayerFamiliarity)
            .ThenBy(c => c.Id)
            .Take(Math.Max(1, limit))
            .ToArray();
    }

    public static decimal SocialCompatibility(this SimulationEngine engine, int citizenId)
    {
        var citizen = engine.State.Citizens.FirstOrDefault(c => c.Id == citizenId && c.Alive);
        return citizen is null ? 0m : Compatibility(engine.State.Player, citizen);
    }

    public static bool MeetPerson(this SimulationEngine engine, int citizenId)
    {
        var state = engine.State;
        var citizen = FindReachableAdult(state, citizenId);
        if (citizen is null) return false;

        if (citizen.PlayerFamiliarity <= 0m)
        {
            citizen.PlayerFamiliarity = 12m;
            citizen.PlayerAffinity = Math.Clamp(18m + Compatibility(state.Player, citizen) * 18m, 0m, 100m);
            citizen.PlayerTrust = 6m;
            citizen.PlayerRelationshipStatus = "Conhecido";
            citizen.LastPlayerInteractionDay = state.CurrentDay;

            engine.AdvanceHours(1);
            state.Player.Social = Math.Clamp(state.Player.Social + 2m, 0m, 100m);
            state.Player.Happiness = Math.Clamp(state.Player.Happiness + 1m, 0m, 100m);

            AddHistory(state, "Social", $"{state.Player.Name} conheceu {citizen.Name}.",
                $"encontro no bairro + compatibilidade {Compatibility(state.Player, citizen):P0} → novo contato", citizen.Id);
            return true;
        }

        return TalkToPerson(engine, citizenId);
    }

    public static bool TalkToPerson(this SimulationEngine engine, int citizenId)
    {
        var state = engine.State;
        var citizen = FindReachableAdult(state, citizenId);
        if (citizen is null || citizen.PlayerFamiliarity <= 0m) return false;

        var compatibility = Compatibility(state.Player, citizen);
        engine.AdvanceHours(1);

        citizen.PlayerFamiliarity = Math.Clamp(citizen.PlayerFamiliarity + 6m, 0m, 100m);
        citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity + 2.5m + compatibility * 3.5m, 0m, 100m);
        citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust + 1.5m + compatibility * 1.5m, 0m, 100m);
        citizen.LastPlayerInteractionDay = state.CurrentDay;

        state.Player.Social = Math.Clamp(state.Player.Social + 2.5m, 0m, 100m);
        state.Player.Happiness = Math.Clamp(state.Player.Happiness + 0.8m, 0m, 100m);
        PromotePlatonicStatus(citizen);

        AddHistory(state, "Social", $"{state.Player.Name} conversou com {citizen.Name}.",
            $"conversa + familiaridade → afinidade {citizen.PlayerAffinity:0}/100, confiança {citizen.PlayerTrust:0}/100", citizen.Id);
        return true;
    }

    public static bool HangOutWithPerson(this SimulationEngine engine, int citizenId)
    {
        var state = engine.State;
        var citizen = FindReachableAdult(state, citizenId);
        if (citizen is null || citizen.PlayerFamiliarity < 15m) return false;

        const decimal leisureCost = 60m;
        if (state.Player.Cash < leisureCost) return false;

        state.Player.Cash -= leisureCost;
        state.Treasury += leisureCost;
        engine.AdvanceHours(3);

        var compatibility = Compatibility(state.Player, citizen);
        citizen.PlayerFamiliarity = Math.Clamp(citizen.PlayerFamiliarity + 10m, 0m, 100m);
        citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity + 6m + compatibility * 7m, 0m, 100m);
        citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust + 4m + compatibility * 4m, 0m, 100m);
        citizen.Happiness = Math.Clamp(citizen.Happiness + 3m, 0m, 100m);
        citizen.Stress = Math.Clamp(citizen.Stress - 2m, 0m, 100m);
        citizen.LastPlayerInteractionDay = state.CurrentDay;

        state.Player.Social = Math.Clamp(state.Player.Social + 8m, 0m, 100m);
        state.Player.Happiness = Math.Clamp(state.Player.Happiness + 4m, 0m, 100m);
        state.Player.Stress = Math.Clamp(state.Player.Stress - 3m, 0m, 100m);
        PromotePlatonicStatus(citizen);

        AddHistory(state, "Social", $"{state.Player.Name} saiu com {citizen.Name}.",
            $"3 horas de lazer + Cr$ {leisureCost:N0} → vínculo social mais forte", citizen.Id);
        return true;
    }

    public static bool FlirtWithPerson(this SimulationEngine engine, int citizenId)
    {
        var state = engine.State;
        var citizen = FindReachableAdult(state, citizenId);
        if (citizen is null || citizen.PlayerFamiliarity < 28m || citizen.PlayerAffinity < 35m) return false;

        if (citizen.PartnerCitizenId is not null && !citizen.IsPlayerPartner)
            return false;

        engine.AdvanceHours(1);
        var compatibility = Compatibility(state.Player, citizen);
        var accepted = citizen.PlayerAffinity >= 42m && citizen.PlayerTrust >= 18m && compatibility >= 0.45m;

        if (accepted)
        {
            citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity + 7m + compatibility * 5m, 0m, 100m);
            citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust + 2m, 0m, 100m);
            if (!citizen.IsPlayerPartner)
                citizen.PlayerRelationshipStatus = "Interesse";
            state.Player.Happiness = Math.Clamp(state.Player.Happiness + 2.5m, 0m, 100m);

            AddHistory(state, "Relacionamento", $"{citizen.Name} correspondeu ao interesse de {state.Player.Name}.",
                $"afinidade {citizen.PlayerAffinity:0}/100 + confiança {citizen.PlayerTrust:0}/100 → interesse romântico", citizen.Id);
        }
        else
        {
            citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity - 4m, 0m, 100m);
            citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust - 2m, 0m, 100m);
            state.Player.Stress = Math.Clamp(state.Player.Stress + 1m, 0m, 100m);

            AddHistory(state, "Relacionamento", $"{citizen.Name} não correspondeu ao flerte de {state.Player.Name}.",
                "afinidade, confiança ou compatibilidade ainda insuficientes", citizen.Id);
        }

        citizen.LastPlayerInteractionDay = state.CurrentDay;
        return accepted;
    }

    public static bool AskToDate(this SimulationEngine engine, int citizenId)
    {
        var state = engine.State;
        var p = state.Player;
        var citizen = FindReachableAdult(state, citizenId);
        if (citizen is null) return false;
        if (p.PartnerCitizenId is not null || p.RelationshipStatus is "Namorando" or "Casado") return false;
        if (citizen.PartnerCitizenId is not null || citizen.IsPlayerPartner) return false;

        var compatibility = Compatibility(p, citizen);
        if (citizen.PlayerFamiliarity < 48m ||
            citizen.PlayerAffinity < 62m ||
            citizen.PlayerTrust < 36m ||
            compatibility < 0.48m)
            return false;

        engine.AdvanceHours(2);

        p.PartnerCitizenId = citizen.Id;
        p.PartnerName = citizen.Name;
        p.RelationshipStatus = "Namorando";
        p.RelationshipStartDay = state.CurrentDay;
        citizen.IsPlayerPartner = true;
        citizen.PlayerRelationshipStatus = "Namorando";
        citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity + 8m, 0m, 100m);
        citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust + 8m, 0m, 100m);
        citizen.LastPlayerInteractionDay = state.CurrentDay;

        p.Happiness = Math.Clamp(p.Happiness + 10m, 0m, 100m);
        citizen.Happiness = Math.Clamp(citizen.Happiness + 8m, 0m, 100m);

        AddHistory(state, "Relacionamento", $"{p.Name} e {citizen.Name} começaram a namorar.",
            "familiaridade + afinidade + confiança + compatibilidade → relacionamento assumido", citizen.Id);
        return true;
    }

    public static bool ProposeMarriage(this SimulationEngine engine)
    {
        var state = engine.State;
        var p = state.Player;
        if (p.RelationshipStatus != "Namorando" || p.PartnerCitizenId is not int partnerId)
            return false;

        var citizen = state.Citizens.FirstOrDefault(c => c.Id == partnerId && c.Alive && c.IsPlayerPartner);
        if (citizen is null) return false;

        var daysTogether = Math.Max(0, state.CurrentDay - p.RelationshipStartDay);
        if (daysTogether < 30 || citizen.PlayerAffinity < 78m || citizen.PlayerTrust < 66m)
            return false;

        const decimal weddingCost = 1_500m;
        if (p.Cash < weddingCost) return false;

        p.Cash -= weddingCost;
        state.Treasury += weddingCost;
        engine.AdvanceHours(6);

        p.RelationshipStatus = "Casado";
        p.MarriageDay = state.CurrentDay;
        citizen.DistrictId = p.DistrictId;
        citizen.PlayerRelationshipStatus = "Cônjuge";
        citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity + 10m, 0m, 100m);
        citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust + 12m, 0m, 100m);
        citizen.LastPlayerInteractionDay = state.CurrentDay;
        p.Happiness = Math.Clamp(p.Happiness + 14m, 0m, 100m);
        citizen.Happiness = Math.Clamp(citizen.Happiness + 12m, 0m, 100m);

        AddHistory(state, "Casamento", $"{p.Name} e {citizen.Name} se casaram.",
            $"relacionamento de {daysTogether} dias + afinidade/confiança altas → casamento; cerimônia Cr$ {weddingCost:N0}", citizen.Id);
        return true;
    }

    public static bool SpendTimeWithPartner(this SimulationEngine engine)
    {
        var state = engine.State;
        if (state.Player.PartnerCitizenId is not int partnerId) return false;
        var citizen = state.Citizens.FirstOrDefault(c => c.Id == partnerId && c.Alive && c.IsPlayerPartner);
        if (citizen is null || citizen.DistrictId != state.Player.DistrictId) return false;

        const decimal cost = 80m;
        if (state.Player.Cash < cost) return false;

        state.Player.Cash -= cost;
        state.Treasury += cost;
        engine.AdvanceHours(4);

        citizen.PlayerAffinity = Math.Clamp(citizen.PlayerAffinity + 6m, 0m, 100m);
        citizen.PlayerTrust = Math.Clamp(citizen.PlayerTrust + 5m, 0m, 100m);
        citizen.LastPlayerInteractionDay = state.CurrentDay;
        citizen.Happiness = Math.Clamp(citizen.Happiness + 4m, 0m, 100m);
        citizen.Stress = Math.Clamp(citizen.Stress - 3m, 0m, 100m);
        state.Player.Happiness = Math.Clamp(state.Player.Happiness + 5m, 0m, 100m);
        state.Player.Stress = Math.Clamp(state.Player.Stress - 4m, 0m, 100m);

        AddHistory(state, "Relacionamento", $"{state.Player.Name} passou tempo de qualidade com {citizen.Name}.",
            "4 horas juntos → afinidade e confiança reforçadas", citizen.Id);
        return true;
    }

    public static bool PlanChild(this SimulationEngine engine)
    {
        var state = engine.State;
        var p = state.Player;
        if (p.RelationshipStatus != "Casado" || p.PartnerCitizenId is not int partnerId)
            return false;

        var partner = state.Citizens.FirstOrDefault(c => c.Id == partnerId && c.Alive && c.IsPlayerPartner);
        if (partner is null || p.AgeYears is < 18 or > 55 || partner.AgeYears is < 18 or > 55)
            return false;

        var daysMarried = Math.Max(0, state.CurrentDay - p.MarriageDay);
        if (daysMarried < 30 || partner.PlayerAffinity < 72m || partner.PlayerTrust < 70m)
            return false;

        const decimal preparationCost = 2_500m;
        if (p.Cash < preparationCost) return false;

        p.Cash -= preparationCost;
        state.Treasury += preparationCost;
        engine.AdvanceHours(8);

        var id = state.NextCitizenId++;
        var child = new CitizenState
        {
            Id = id,
            Name = $"Filho(a) {id:000}",
            AgeDays = 0,
            Cash = 0m,
            DistrictId = p.DistrictId,
            HouseholdId = 0,
            SkillTier = 1,
            EducationLevel = 0,
            Hunger = 8m,
            Energy = 90m,
            Happiness = 78m,
            Stress = 4m,
            Ambition = (p.Ambition + partner.Ambition) / 2m,
            Sociability = (p.Sociability + partner.Sociability) / 2m,
            Discipline = (p.Discipline + partner.Discipline) / 2m,
            RiskTolerance = (p.RiskTolerance + partner.RiskTolerance) / 2m,
            CurrentActivity = "Em casa"
        };
        state.Citizens.Add(child);
        p.Children++;
        p.Happiness = Math.Clamp(p.Happiness + 12m, 0m, 100m);
        partner.Happiness = Math.Clamp(partner.Happiness + 12m, 0m, 100m);

        AddHistory(state, "Família", $"{p.Name} e {partner.Name} ampliaram a família.",
            $"casamento estável + planejamento Cr$ {preparationCost:N0} → novo membro do domicílio", child.Id);
        return true;
    }

    public static bool BreakUp(this SimulationEngine engine)
    {
        var state = engine.State;
        var p = state.Player;
        if (p.PartnerCitizenId is not int partnerId) return false;

        var partner = state.Citizens.FirstOrDefault(c => c.Id == partnerId);
        engine.AdvanceHours(2);

        if (partner is not null)
        {
            partner.IsPlayerPartner = false;
            partner.PlayerRelationshipStatus = "Ex";
            partner.PlayerAffinity = Math.Clamp(partner.PlayerAffinity - 22m, 0m, 100m);
            partner.PlayerTrust = Math.Clamp(partner.PlayerTrust - 28m, 0m, 100m);
            partner.Happiness = Math.Clamp(partner.Happiness - 9m, 0m, 100m);
            partner.Stress = Math.Clamp(partner.Stress + 10m, 0m, 100m);
            partner.LastPlayerInteractionDay = state.CurrentDay;
        }

        var oldName = p.PartnerName ?? partner?.Name ?? "parceiro";
        p.PartnerCitizenId = null;
        p.PartnerName = null;
        p.RelationshipStatus = "Solteiro";
        p.RelationshipStartDay = -1;
        p.MarriageDay = -1;
        p.Happiness = Math.Clamp(p.Happiness - 8m, 0m, 100m);
        p.Stress = Math.Clamp(p.Stress + 8m, 0m, 100m);

        AddHistory(state, "Relacionamento", $"{p.Name} encerrou o relacionamento com {oldName}.",
            "decisão do jogador → vínculo romântico encerrado", partnerId);
        return true;
    }

    public static string RelationshipRequirements(this SimulationEngine engine, int citizenId)
    {
        var state = engine.State;
        var c = state.Citizens.FirstOrDefault(x => x.Id == citizenId && x.Alive);
        if (c is null) return "Pessoa indisponível.";

        if (state.Player.PartnerCitizenId == c.Id)
        {
            if (state.Player.RelationshipStatus == "Namorando")
            {
                var days = Math.Max(0, state.CurrentDay - state.Player.RelationshipStartDay);
                return $"Namoro: {days} dias • casar exige 30 dias, afinidade 78 e confiança 66.";
            }
            if (state.Player.RelationshipStatus == "Casado")
                return $"Cônjuge • afinidade {c.PlayerAffinity:0} • confiança {c.PlayerTrust:0}.";
        }

        if (c.PlayerFamiliarity <= 0m)
            return "Ainda não se conhecem.";

        if (c.PlayerFamiliarity < 28m || c.PlayerAffinity < 35m)
            return "Flerte: familiaridade 28 e afinidade 35.";

        if (c.PlayerFamiliarity < 48m || c.PlayerAffinity < 62m || c.PlayerTrust < 36m)
            return "Namoro: familiaridade 48, afinidade 62 e confiança 36.";

        return "Há base suficiente para tentar iniciar um namoro.";
    }

    private static CitizenState? FindReachableAdult(GameState state, int citizenId)
    {
        return state.Citizens.FirstOrDefault(c =>
            c.Id == citizenId &&
            c.Alive &&
            c.AgeYears >= 18 &&
            c.DistrictId == state.Player.DistrictId &&
            c.CurrentActivity != "Dormindo");
    }

    private static decimal Compatibility(PlayerState p, CitizenState c)
    {
        var difference =
            Math.Abs(p.Ambition - c.Ambition) * 0.28m +
            Math.Abs(p.Sociability - c.Sociability) * 0.32m +
            Math.Abs(p.Discipline - c.Discipline) * 0.22m +
            Math.Abs(p.RiskTolerance - c.RiskTolerance) * 0.18m;

        var lifeFit =
            Math.Clamp(1m - Math.Abs(p.AgeYears - c.AgeYears) / 30m, 0m, 1m) * 0.10m +
            Math.Clamp(c.Happiness / 100m, 0m, 1m) * 0.05m;

        return Math.Clamp(1m - difference + lifeFit, 0m, 1m);
    }

    private static void PromotePlatonicStatus(CitizenState citizen)
    {
        if (citizen.IsPlayerPartner) return;
        if (citizen.PlayerRelationshipStatus == "Interesse") return;

        citizen.PlayerRelationshipStatus =
            citizen.PlayerTrust >= 58m && citizen.PlayerAffinity >= 68m ? "Amigo próximo" :
            citizen.PlayerTrust >= 34m && citizen.PlayerAffinity >= 50m ? "Amigo" :
            citizen.PlayerFamiliarity >= 12m ? "Conhecido" :
            "Desconhecido";
    }

    private static void AddHistory(GameState state, string kind, string summary, string cause, int? entityId = null)
    {
        state.History.Add(new HistoryEvent
        {
            Day = state.CurrentDay,
            Kind = kind,
            Summary = summary,
            Cause = cause,
            EntityId = entityId
        });
    }
}
