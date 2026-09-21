using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class CityView : Control
{
    private SimulationEngine? _engine;
    private double _anim;
    private double _redrawAccumulator;
    private readonly AdaptiveGraphicsController _graphics = new();
    private GraphicsBudget _budget = AdaptiveGraphicsController.BudgetFor(GraphicsProfile.Balanced);

    private readonly Color _nightSky = new(0.010f, 0.020f, 0.045f);
    private readonly Color _daySky = new(0.055f, 0.135f, 0.185f);
    private readonly Color _road = new(0.055f, 0.105f, 0.135f);
    private readonly Color _roadLine = new(0.20f, 0.34f, 0.42f);
    private readonly Color _accent = new(0.24f, 0.84f, 0.82f);
    private readonly Color _gold = new(0.96f, 0.72f, 0.29f);
    private readonly Color _lamp = new(1.0f, 0.84f, 0.48f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcess(true);
    }

    public void SetEngine(SimulationEngine engine)
    {
        _engine = engine;
        QueueRedraw();
    }

    public void SetGraphicsProfile(GraphicsProfile profile)
    {
        _graphics.SetProfile(profile);
        _budget = _graphics.Budget;
        QueueRedraw();
    }

    public GraphicsProfile RequestedGraphicsProfile => _graphics.Requested;
    public GraphicsProfile EffectiveGraphicsProfile => _graphics.Effective;
    public string GraphicsStatusText => _graphics.StatusText();

    public override void _Process(double delta)
    {
        _anim += delta;
        _redrawAccumulator += delta;
        if (_anim > 10000) _anim = 0;

        if (_graphics.Update(delta))
        {
            _budget = _graphics.Budget;
            QueueRedraw();
        }

        var hz = Math.Max(10, _graphics.Budget.RedrawHz);
        if (_redrawAccumulator >= 1.0 / hz)
        {
            _redrawAccumulator = 0;
            _budget = _graphics.Budget;
            QueueRedraw();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        if (_engine is null)
        {
            DrawRect(new Rect2(Vector2.Zero, size), _nightSky, true);
            return;
        }

        var state = _engine.State;
        var daylight = Daylight(state.CurrentHour);
        DrawAtmosphere(size, state, daylight);

        var usableW = MathF.Max(520f, size.X - 72f);
        var tileW = MathF.Min(228f, usableW / 3.55f);
        var tileH = tileW * 0.48f;
        var center = new Vector2(size.X * 0.50f, MathF.Max(160f, size.Y * 0.245f));

        DrawCityBase(center, tileW, tileH, daylight);

        foreach (var district in state.Districts.OrderBy(d => d.GridX + d.GridY))
        {
            var p = DistrictCenter(center, district.GridX, district.GridY, tileW, tileH);
            DrawDistrict(state, district, p, tileW, tileH, daylight);
        }

        DrawPrimaryRoads(center, tileW, tileH, daylight);
        DrawStreetLights(state, center, tileW, tileH, daylight);
        DrawTraffic(state, center, tileW, tileH, daylight);
        DrawPedestrians(state, center, tileW, tileH, daylight);
        DrawWeatherOverlay(size, state, daylight);
        DrawEconomicPulse(state, center, tileW, tileH, daylight);
        DrawVignette(size);
    }

    private void DrawAtmosphere(Vector2 size, GameState state, float daylight)
    {
        var sky = _nightSky.Lerp(_daySky, daylight);
        var horizon = sky.Lightened(0.10f + daylight * 0.08f);

        const int bands = 18;
        for (var i = 0; i < bands; i++)
        {
            var t = i / (float)(bands - 1);
            var color = sky.Lerp(horizon, t * 0.78f);
            DrawRect(new Rect2(0, size.Y * t, size.X, size.Y / bands + 2), color, true);
        }

        if (daylight < 0.38f)
            DrawStars(size, 1f - daylight / 0.38f);

        DrawSunMoon(size, state.CurrentHour, daylight);

        if (_budget.DrawCityGrid)
        {
            var gridAlpha = Mathf.Lerp(0.12f, 0.055f, daylight);
            var grid = new Color(0.18f, 0.36f, 0.44f, gridAlpha);
            for (var x = -400; x < size.X + 400; x += 62)
            {
                DrawLine(new Vector2(x, size.Y), new Vector2(x + size.Y, 0), grid, 1f);
                DrawLine(new Vector2(x, 0), new Vector2(x + size.Y, size.Y), grid, 1f);
            }
        }
    }

    private void DrawStars(Vector2 size, float alpha)
    {
        for (var i = 0; i < _budget.Stars; i++)
        {
            var h = StableHash($"star:{i}");
            var x = ((h & 0xFFFF) / 65535f) * size.X;
            var y = (((h >> 16) & 0x7FFF) / 32767f) * size.Y * 0.55f;
            var radius = 0.7f + ((h >> 8) & 0x3) * 0.25f;
            DrawCircle(new Vector2(x, y), radius, new Color(0.75f, 0.88f, 1f, alpha * 0.72f));
        }
    }

    private void DrawSunMoon(Vector2 size, int hour, float daylight)
    {
        var t = hour / 24f;
        var x = Mathf.Lerp(size.X * 0.12f, size.X * 0.88f, t);
        var arc = MathF.Sin(t * MathF.PI);
        var y = size.Y * 0.28f - arc * size.Y * 0.17f;

        if (daylight > 0.52f)
        {
            DrawCircle(new Vector2(x, y), 18f, new Color(1f, 0.76f, 0.28f, 0.12f));
            DrawCircle(new Vector2(x, y), 8f, new Color(1f, 0.86f, 0.46f, 0.86f));
        }
        else
        {
            var moonX = size.X - x;
            DrawCircle(new Vector2(moonX, y), 11f, new Color(0.78f, 0.86f, 1f, 0.56f));
            DrawCircle(new Vector2(moonX + 4f, y - 2f), 9f, _nightSky);
        }
    }

    private void DrawCityBase(Vector2 center, float tileW, float tileH, float daylight)
    {
        var c = center + new Vector2(0, tileH * 1.52f);
        var points = Diamond(c, tileW * 3.38f, tileH * 3.38f);
        var shadow = points.Select(p => p + new Vector2(0, 19)).ToArray();

        DrawColoredPolygon(shadow, new Color(0, 0, 0, 0.38f));
        DrawColoredPolygon(points, new Color(0.020f + daylight * 0.016f, 0.060f + daylight * 0.020f, 0.075f + daylight * 0.025f));

        for (var i = 0; i < points.Length; i++)
            DrawLine(points[i], points[(i + 1) % points.Length], new Color(0.12f, 0.28f, 0.35f, 0.90f), 2f, true);
    }

    private void DrawDistrict(GameState state, DistrictState district, Vector2 p, float tileW, float tileH, float daylight)
    {
        var points = Diamond(p, tileW * 0.93f, tileH * 0.91f);
        var selected = district.Id == state.Player.DistrictId;
        var wealth = (float)Math.Clamp(district.WealthIndex, 0.7m, 1.6m);

        var baseR = 0.042f + wealth * 0.020f + daylight * 0.012f;
        var baseG = 0.090f + wealth * 0.032f + daylight * 0.028f;
        var baseB = 0.115f + wealth * 0.038f + daylight * 0.030f;
        var color = selected
            ? new Color(0.060f + daylight * 0.025f, 0.245f + daylight * 0.035f, 0.285f + daylight * 0.030f)
            : new Color(baseR, baseG, baseB);

        DrawColoredPolygon(points.Select(v => v + new Vector2(0, 7)).ToArray(), new Color(0, 0, 0, 0.31f));
        DrawColoredPolygon(points, color);

        var edge = selected ? _accent : new Color(0.15f, 0.31f, 0.38f);
        for (var i = 0; i < points.Length; i++)
            DrawLine(points[i], points[(i + 1) % points.Length], edge, selected ? 2.5f : 1.2f, true);

        DrawDistrictRoads(p, tileW, tileH, daylight);
        DrawDistrictParks(district, p, tileW, tileH, daylight);

        var companies = state.Companies
            .Where(c => c.Open && c.DistrictId == district.Id)
            .OrderBy(c => c.Id)
            .Take(_budget.MaxCompaniesPerDistrict)
            .ToArray();

        for (var i = 0; i < companies.Length; i++)
        {
            var row = i / 7;
            var col = i % 7;
            var localX = (col - 3f) * (tileW * 0.092f);
            var localY = (row - 1.55f) * (tileH * 0.128f);
            var footprint = p + new Vector2(localX - localY, (localX + localY) * 0.45f + tileH * 0.025f);
            var scale = 0.82f + (float)companies[i].Reputation * 0.35f;
            var h = (15f + (companies[i].Id % 8) * 5f + wealth * 5f) * scale;
            var width = (9f + (companies[i].Id % 4) * 2f) * scale;
            var buildingColor = companies[i].PlayerOwned ? _gold : SectorColor(companies[i].Sector);
            DrawBuilding(footprint, width, h, buildingColor, companies[i], daylight);
        }

        DrawTrees(p, tileW, tileH, district.Id, daylight);
    }

    private void DrawDistrictParks(DistrictState district, Vector2 p, float tileW, float tileH, float daylight)
    {
        var green = new Color(0.08f, 0.27f + daylight * 0.06f, 0.20f + daylight * 0.04f, 0.80f);
        var count = district.SocialIndex > 1.08m ? 2 : 1;
        for (var i = 0; i < count; i++)
        {
            var offset = i == 0
                ? new Vector2(-tileW * 0.23f, tileH * 0.11f)
                : new Vector2(tileW * 0.20f, -tileH * 0.08f);
            DrawColoredPolygon(Diamond(p + offset, tileW * 0.18f, tileH * 0.17f), green);
        }
    }

    private void DrawDistrictRoads(Vector2 p, float tileW, float tileH, float daylight)
    {
        var a1 = p + new Vector2(-tileW * 0.35f, 0);
        var a2 = p + new Vector2(tileW * 0.35f, 0);
        var b1 = p + new Vector2(0, -tileH * 0.35f);
        var b2 = p + new Vector2(0, tileH * 0.35f);

        var road = new Color(0.025f + daylight * 0.018f, 0.060f + daylight * 0.020f, 0.080f + daylight * 0.024f, 0.88f);
        DrawLine(a1, a2, road, 5.5f, true);
        DrawLine(b1, b2, road, 5.5f, true);
        DrawLine(a1, a2, new Color(0.25f, 0.38f, 0.44f, 0.24f), 1f, true);
        DrawLine(b1, b2, new Color(0.25f, 0.38f, 0.44f, 0.24f), 1f, true);
    }

    private void DrawPrimaryRoads(Vector2 center, float tileW, float tileH, float daylight)
    {
        var c = center + new Vector2(0, tileH * 1.52f);
        var spanX = tileW * 1.54f;
        var spanY = tileH * 1.52f;
        var road = _road.Lerp(new Color(0.09f, 0.15f, 0.18f), daylight);

        DrawLine(c + new Vector2(-spanX, -spanY), c + new Vector2(spanX, spanY), road, 11f, true);
        DrawLine(c + new Vector2(spanX, -spanY), c + new Vector2(-spanX, spanY), road, 11f, true);
        DrawLine(c + new Vector2(-spanX, -spanY), c + new Vector2(spanX, spanY), _roadLine, 1.3f, true);
        DrawLine(c + new Vector2(spanX, -spanY), c + new Vector2(-spanX, spanY), _roadLine, 1.3f, true);
    }

    private void DrawStreetLights(GameState state, Vector2 center, float tileW, float tileH, float daylight)
    {
        if (daylight > 0.55f) return;

        var c = center + new Vector2(0, tileH * 1.52f);
        var darkness = 1f - daylight;
        for (var i = 0; i < (_budget.DrawStreetGlow ? 14 : 8); i++)
        {
            var t = i / 13f;
            var x = Mathf.Lerp(-tileW * 1.43f, tileW * 1.43f, t);
            var y = x * (tileH / tileW);
            var p1 = c + new Vector2(x, y) + new Vector2(-4, -3);
            var p2 = c + new Vector2(x, -y) + new Vector2(4, -3);

            DrawCircle(p1, 5.2f, new Color(_lamp.R, _lamp.G, _lamp.B, 0.08f * darkness));
            DrawCircle(p1, 1.8f, new Color(_lamp.R, _lamp.G, _lamp.B, 0.86f * darkness));
            DrawCircle(p2, 5.2f, new Color(_lamp.R, _lamp.G, _lamp.B, 0.08f * darkness));
            DrawCircle(p2, 1.8f, new Color(_lamp.R, _lamp.G, _lamp.B, 0.86f * darkness));
        }
    }

    private void DrawTraffic(GameState state, Vector2 center, float tileW, float tileH, float daylight)
    {
        var c = center + new Vector2(0, tileH * 1.52f);
        var span = tileW * 1.42f;
        var rush = state.CurrentHour is >= 7 and <= 9 or >= 16 and <= 19 ? 1.0f : state.CurrentHour is >= 0 and <= 5 ? 0.30f : 0.65f;
        var countA = Math.Max(3, (int)(15 * rush * _budget.TrafficScale));
        var countB = Math.Max(2, (int)(12 * rush * _budget.TrafficScale));
        var phase = (float)((_anim * (0.065 + rush * 0.055)) % 1.0);

        for (var i = 0; i < countA; i++)
        {
            var t = (phase + i / (float)countA) % 1f;
            var x = Mathf.Lerp(-span, span, t);
            var y = x * (tileH / tileW);
            var p = c + new Vector2(x, y);
            DrawCircle(p, 2.4f, i % 4 == 0 ? _gold : new Color(0.44f, 0.70f, 0.82f));
            if (daylight < 0.45f && _budget.DrawAmbientTrafficLights)
            {
                DrawCircle(p + new Vector2(2, 1), 2.8f, new Color(1f, 0.82f, 0.42f, 0.10f));
                DrawCircle(p + new Vector2(2, 1), 0.9f, new Color(1f, 0.92f, 0.68f, 0.85f));
            }
        }

        for (var i = 0; i < countB; i++)
        {
            var t = ((1f - phase) + i / (float)countB) % 1f;
            var x = Mathf.Lerp(span, -span, t);
            var y = -x * (tileH / tileW);
            var p = c + new Vector2(x, y);
            DrawCircle(p, 2.2f, i % 3 == 0 ? _accent : new Color(0.58f, 0.64f, 0.72f));
        }
    }

    private void DrawPedestrians(GameState state, Vector2 center, float tileW, float tileH, float daylight)
    {
        if (state.CurrentHour is >= 0 and < 6) return;

        var activityFactor = state.CurrentHour is >= 7 and <= 20 ? 1f : 0.45f;
        foreach (var district in state.Districts)
        {
            var p = DistrictCenter(center, district.GridX, district.GridY, tileW, tileH);
            var residents = state.Citizens.Count(c => c.Alive && c.DistrictId == district.Id);
            var count = Math.Clamp((int)(residents / (float)_budget.ResidentPedestrianDivisor * activityFactor), 1, _budget.MaxPedestriansPerDistrict);

            for (var i = 0; i < count; i++)
            {
                var h = StableHash($"ped:{district.Id}:{i}");
                var phase = (float)((_anim * (0.03 + ((h & 7) * 0.004)) + i * 0.13) % 1.0);
                var startX = ((h & 0xFF) / 255f - 0.5f) * tileW * 0.42f;
                var endX = -startX;
                var x = Mathf.Lerp(startX, endX, phase);
                var baseY = (((h >> 8) & 0xFF) / 255f - 0.5f) * tileH * 0.22f;
                var y = baseY + x * 0.38f;
                var person = p + new Vector2(x, y);

                var color = (h & 1) == 0 ? new Color(0.78f, 0.89f, 0.95f, 0.82f) : new Color(0.96f, 0.68f, 0.38f, 0.82f);
                DrawCircle(person, 1.35f, color);
                DrawLine(person + new Vector2(0, 1), person + new Vector2(0, 4), color.Darkened(0.20f), 1f);
            }
        }
    }

    private void DrawBuilding(Vector2 basePoint, float width, float height, Color color, CompanyState company, float daylight)
    {
        var top = basePoint - new Vector2(0, height);
        var half = width * 0.5f;
        var depth = width * 0.42f;

        var topFace = new[]
        {
            top + new Vector2(0, -depth),
            top + new Vector2(half, 0),
            top + new Vector2(0, depth),
            top + new Vector2(-half, 0)
        };
        var leftFace = new[]
        {
            topFace[3],
            topFace[2],
            basePoint + new Vector2(0, depth),
            basePoint + new Vector2(-half, 0)
        };
        var rightFace = new[]
        {
            topFace[2],
            topFace[1],
            basePoint + new Vector2(half, 0),
            basePoint + new Vector2(0, depth)
        };

        var nightBoost = 1f - daylight;
        DrawColoredPolygon(leftFace, color.Darkened(0.38f - daylight * 0.08f));
        DrawColoredPolygon(rightFace, color.Darkened(0.25f - daylight * 0.06f));
        DrawColoredPolygon(topFace, color.Lightened(0.12f + daylight * 0.06f));

        if (company.PlayerOwned)
        {
            DrawLine(topFace[3], topFace[0], _gold, 2f, true);
            DrawLine(topFace[0], topFace[1], _gold, 2f, true);
            DrawCircle(top - new Vector2(0, depth + 5f), 3.2f, new Color(_gold.R, _gold.G, _gold.B, 0.30f));
            DrawCircle(top - new Vector2(0, depth + 5f), 1.7f, _gold);
        }

        if (height > 26)
        {
            var rows = Math.Min(_budget.WindowRows, (int)(height / 12));
            for (var r = 1; r <= rows; r++)
            {
                var y = top.Y + r * (height / (rows + 1));
                var lit = ((company.Id + r) % 3 != 0) && nightBoost > 0.25f;
                var window = lit
                    ? new Color(1f, 0.82f, 0.42f, 0.45f + nightBoost * 0.45f)
                    : new Color(0.42f, 0.66f, 0.76f, 0.28f + daylight * 0.30f);

                DrawLine(new Vector2(basePoint.X - half + 2, y), new Vector2(basePoint.X - 1, y + 1.3f), window, 1.1f);
                DrawLine(new Vector2(basePoint.X + 1, y + 1.3f), new Vector2(basePoint.X + half - 2, y), window, 1.1f);
            }
        }

        if (_budget.DrawBuildingDetails && height > 45 && company.Innovation > 0.55m)
        {
            DrawLine(top + new Vector2(0, -depth), top + new Vector2(0, -depth - 8), color.Lightened(0.25f), 1f);
            DrawCircle(top + new Vector2(0, -depth - 9), 1.3f, _accent);
        }
    }

    private void DrawTrees(Vector2 p, float tileW, float tileH, int districtId, float daylight)
    {
        for (var i = 0; i < _budget.TreesPerDistrict; i++)
        {
            var hash = StableHash($"{districtId}:{i}");
            var fx = ((hash & 0xFF) / 255f - 0.5f) * tileW * 0.57f;
            var fy = (((hash >> 8) & 0xFF) / 255f - 0.5f) * tileH * 0.43f;
            var tree = p + new Vector2(fx - fy, (fx + fy) * 0.44f);

            DrawLine(tree + new Vector2(2, 2), tree + new Vector2(5, 6), new Color(0, 0, 0, 0.22f), 2f);
            DrawLine(tree, tree - new Vector2(0, 7), new Color(0.29f, 0.23f, 0.17f), 2f);
            var leaf = new Color(0.14f + daylight * 0.04f, 0.36f + daylight * 0.14f, 0.28f + daylight * 0.08f);
            DrawCircle(tree - new Vector2(0, 10), 4.3f, leaf);
            DrawCircle(tree - new Vector2(3, 8), 3.2f, leaf.Darkened(0.18f));
        }
    }

    private void DrawWeatherOverlay(Vector2 size, GameState state, float daylight)
    {
        if (state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase))
        {
            var heavy = state.Weather.Contains("forte", StringComparison.OrdinalIgnoreCase);
            var count = heavy ? _budget.RainParticles : Math.Max(18, (int)(_budget.RainParticles * 0.60f));
            var speed = heavy ? 360f : 260f;

            for (var i = 0; i < count; i++)
            {
                var h = StableHash($"rain:{i}");
                var xBase = ((h & 0xFFFF) / 65535f) * (size.X + 100f) - 50f;
                var yBase = (((h >> 16) & 0x7FFF) / 32767f) * size.Y;
                var y = (yBase + (float)_anim * speed) % size.Y;
                var x = (xBase + y * 0.12f) % (size.X + 60f) - 30f;
                var alpha = heavy ? 0.30f : 0.20f;
                DrawLine(new Vector2(x, y), new Vector2(x - 4f, y + (heavy ? 13f : 9f)), new Color(0.62f, 0.80f, 0.92f, alpha), 1f);
            }

            DrawRect(new Rect2(Vector2.Zero, size), new Color(0.03f, 0.08f, 0.12f, heavy ? 0.13f : 0.07f), true);
        }
        else if (state.Weather.Contains("Neblina", StringComparison.OrdinalIgnoreCase) && _budget.DrawWeatherAtmosphere)
        {
            DrawRect(new Rect2(Vector2.Zero, size), new Color(0.70f, 0.78f, 0.80f, 0.09f + (1f - daylight) * 0.03f), true);
            for (var i = 0; i < 5; i++)
            {
                var y = size.Y * (0.25f + i * 0.13f);
                var drift = (float)Math.Sin(_anim * 0.08 + i) * 45f;
                DrawRect(new Rect2(-80 + drift, y, size.X + 160, 28), new Color(0.76f, 0.83f, 0.84f, 0.025f), true);
            }
        }
    }

    private void DrawEconomicPulse(GameState state, Vector2 center, float tileW, float tileH, float daylight)
    {
        if (!_budget.DrawBuildingDetails) return;

        foreach (var district in state.Districts)
        {
            var companies = state.Companies.Where(c => c.Open && c.DistrictId == district.Id).ToArray();
            if (companies.Length == 0) continue;

            var p = DistrictCenter(center, district.GridX, district.GridY, tileW, tileH);
            var healthy = companies.Count(c => c.OperatingStatus == "Ativa");
            var crisis = companies.Count(c => c.OperatingStatus == "Crise");
            var avgShare = companies.Average(c => c.MarketShare);
            var activity = Math.Clamp(healthy / (float)companies.Length + (float)avgShare * 0.8f - crisis * 0.03f, 0.12f, 1.25f);

            var pulse = 0.5f + 0.5f * MathF.Sin((float)_anim * 1.6f + district.Id);
            var radius = tileW * (0.20f + activity * 0.035f) + pulse * 2f;
            var alpha = 0.015f + activity * 0.015f;
            var color = crisis > healthy / 2
                ? new Color(1.0f, 0.34f, 0.24f, alpha)
                : new Color(0.24f, 0.84f, 0.82f, alpha);

            DrawCircle(p + new Vector2(0, tileH * 0.02f), radius, color);
        }

        if (state.Player.BusinessCompanyId is int companyId)
        {
            var company = state.Companies.FirstOrDefault(c => c.Id == companyId && c.Open);
            if (company is not null)
            {
                var district = state.Districts.First(d => d.Id == company.DistrictId);
                var p = DistrictCenter(center, district.GridX, district.GridY, tileW, tileH);
                var pulse = 0.5f + 0.5f * MathF.Sin((float)_anim * 3.1f);
                DrawArc(p - new Vector2(0, tileH * 0.10f), 18f + pulse * 4f, 0, MathF.Tau, 36, new Color(_gold.R, _gold.G, _gold.B, 0.45f), 1.8f, true);
            }
        }
    }

    private void DrawVignette(Vector2 size)
    {
        var edge = new Color(0, 0, 0, 0.12f);
        DrawRect(new Rect2(0, 0, size.X, 9), edge, true);
        DrawRect(new Rect2(0, size.Y - 9, size.X, 9), edge, true);
        DrawRect(new Rect2(0, 0, 9, size.Y), edge, true);
        DrawRect(new Rect2(size.X - 9, 0, 9, size.Y), edge, true);
    }

    private static float Daylight(int hour)
    {
        if (hour < 5 || hour >= 22) return 0.08f;
        if (hour < 7) return Mathf.Lerp(0.08f, 0.72f, (hour - 5) / 2f);
        if (hour < 18) return 1f;
        if (hour < 21) return Mathf.Lerp(1f, 0.16f, (hour - 18) / 3f);
        return 0.10f;
    }

    private static Vector2 DistrictCenter(Vector2 center, int gridX, int gridY, float tileW, float tileH)
    {
        var isoX = (gridX - gridY) * tileW * 0.50f;
        var isoY = (gridX + gridY) * tileH * 0.50f;
        return center + new Vector2(isoX, isoY);
    }

    private static Vector2[] Diamond(Vector2 center, float width, float height) =>
    [
        center + new Vector2(0, -height / 2),
        center + new Vector2(width / 2, 0),
        center + new Vector2(0, height / 2),
        center + new Vector2(-width / 2, 0)
    ];

    private static Color SectorColor(string sector)
    {
        var palette = new[]
        {
            new Color(0.29f, 0.64f, 0.87f),
            new Color(0.37f, 0.78f, 0.66f),
            new Color(0.54f, 0.49f, 0.80f),
            new Color(0.84f, 0.54f, 0.66f),
            new Color(0.89f, 0.65f, 0.37f),
            new Color(0.45f, 0.65f, 0.64f),
            new Color(0.48f, 0.59f, 0.81f),
            new Color(0.65f, 0.70f, 0.48f)
        };
        return palette[StableHash(sector) % palette.Length];
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in text) hash = hash * 31 + c;
            return hash & 0x7FFFFFFF;
        }
    }
}
