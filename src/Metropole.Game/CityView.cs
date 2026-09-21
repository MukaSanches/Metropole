using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class CityView : Control
{
    private SimulationEngine? _engine;
    private double _anim;

    private readonly Color _sky = new(0.018f, 0.040f, 0.060f);
    private readonly Color _ground = new(0.045f, 0.095f, 0.125f);
    private readonly Color _road = new(0.08f, 0.15f, 0.19f);
    private readonly Color _roadLine = new(0.20f, 0.31f, 0.38f);
    private readonly Color _accent = new(0.24f, 0.84f, 0.82f);
    private readonly Color _gold = new(0.96f, 0.72f, 0.29f);

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

    public override void _Process(double delta)
    {
        _anim += delta;
        if (_anim > 10000) _anim = 0;
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), _sky, true);
        DrawBackdrop(size);

        if (_engine is null) return;

        var state = _engine.State;
        var usableW = MathF.Max(500f, size.X - 70f);
        var tileW = MathF.Min(220f, usableW / 3.6f);
        var tileH = tileW * 0.48f;
        var center = new Vector2(size.X * 0.50f, MathF.Max(165f, size.Y * 0.25f));

        DrawCityBase(center, tileW, tileH);

        foreach (var district in state.Districts.OrderBy(d => d.GridX + d.GridY))
        {
            var p = DistrictCenter(center, district.GridX, district.GridY, tileW, tileH);
            DrawDistrict(state, district, p, tileW, tileH);
        }

        DrawPrimaryRoads(center, tileW, tileH);
        DrawTraffic(center, tileW, tileH);
        DrawVignette(size);
    }

    private void DrawBackdrop(Vector2 size)
    {
        const int bands = 16;
        for (var i = 0; i < bands; i++)
        {
            var t = i / (float)bands;
            DrawRect(
                new Rect2(0, size.Y * t, size.X, size.Y / bands + 2),
                _sky.Lerp(_ground, t * 0.42f),
                true);
        }

        var grid = new Color(0.11f, 0.22f, 0.29f, 0.14f);
        for (var x = -400; x < size.X + 400; x += 60)
        {
            DrawLine(new Vector2(x, size.Y), new Vector2(x + size.Y, 0), grid, 1f);
            DrawLine(new Vector2(x, 0), new Vector2(x + size.Y, size.Y), grid, 1f);
        }
    }

    private void DrawCityBase(Vector2 center, float tileW, float tileH)
    {
        var c = center + new Vector2(0, tileH * 1.52f);
        var points = Diamond(c, tileW * 3.35f, tileH * 3.35f);
        var shadow = points.Select(p => p + new Vector2(0, 17)).ToArray();

        DrawColoredPolygon(shadow, new Color(0, 0, 0, 0.35f));
        DrawColoredPolygon(points, new Color(0.025f, 0.07f, 0.09f));

        for (var i = 0; i < points.Length; i++)
            DrawLine(points[i], points[(i + 1) % points.Length], new Color(0.11f, 0.23f, 0.30f), 2f);
    }

    private void DrawDistrict(GameState state, DistrictState district, Vector2 p, float tileW, float tileH)
    {
        var points = Diamond(p, tileW * 0.93f, tileH * 0.91f);
        var selected = district.Id == state.Player.DistrictId;
        var wealth = (float)Math.Clamp(district.WealthIndex, 0.7m, 1.6m);

        var color = selected
            ? new Color(0.07f, 0.28f, 0.31f)
            : new Color(0.045f + wealth * 0.024f, 0.10f + wealth * 0.030f, 0.13f + wealth * 0.034f);

        DrawColoredPolygon(points.Select(v => v + new Vector2(0, 6)).ToArray(), new Color(0, 0, 0, 0.28f));
        DrawColoredPolygon(points, color);

        var edge = selected ? _accent : new Color(0.15f, 0.29f, 0.36f);
        for (var i = 0; i < points.Length; i++)
            DrawLine(points[i], points[(i + 1) % points.Length], edge, selected ? 2.3f : 1.1f);

        DrawDistrictRoads(p, tileW, tileH);

        var companies = state.Companies
            .Where(c => c.Open && c.DistrictId == district.Id)
            .OrderBy(c => c.Id)
            .Take(24)
            .ToArray();

        for (var i = 0; i < companies.Length; i++)
        {
            var row = i / 6;
            var col = i % 6;
            var localX = (col - 2.5f) * (tileW * 0.105f);
            var localY = (row - 1.5f) * (tileH * 0.135f);
            var footprint = p + new Vector2(localX - localY, (localX + localY) * 0.45f + tileH * 0.02f);
            var h = 15f + (companies[i].Id % 7) * 5f + wealth * 5f;
            var width = 10f + (companies[i].Id % 3) * 2f;
            var buildingColor = companies[i].PlayerOwned ? _gold : SectorColor(companies[i].Sector);
            DrawBuilding(footprint, width, h, buildingColor, companies[i].Id);
        }

        DrawTrees(p, tileW, tileH, district.Id);
    }

    private void DrawDistrictRoads(Vector2 p, float tileW, float tileH)
    {
        var a1 = p + new Vector2(-tileW * 0.34f, 0);
        var a2 = p + new Vector2(tileW * 0.34f, 0);
        var b1 = p + new Vector2(0, -tileH * 0.34f);
        var b2 = p + new Vector2(0, tileH * 0.34f);

        DrawLine(a1, a2, new Color(0.03f, 0.07f, 0.09f, 0.75f), 5f);
        DrawLine(b1, b2, new Color(0.03f, 0.07f, 0.09f, 0.75f), 5f);
        DrawLine(a1, a2, new Color(0.22f, 0.34f, 0.40f, 0.28f), 1f);
        DrawLine(b1, b2, new Color(0.22f, 0.34f, 0.40f, 0.28f), 1f);
    }

    private void DrawPrimaryRoads(Vector2 center, float tileW, float tileH)
    {
        var c = center + new Vector2(0, tileH * 1.52f);
        var spanX = tileW * 1.52f;
        var spanY = tileH * 1.50f;

        DrawLine(c + new Vector2(-spanX, -spanY), c + new Vector2(spanX, spanY), _road, 10f);
        DrawLine(c + new Vector2(spanX, -spanY), c + new Vector2(-spanX, spanY), _road, 10f);
        DrawLine(c + new Vector2(-spanX, -spanY), c + new Vector2(spanX, spanY), _roadLine, 1.2f);
        DrawLine(c + new Vector2(spanX, -spanY), c + new Vector2(-spanX, spanY), _roadLine, 1.2f);
    }

    private void DrawTraffic(Vector2 center, float tileW, float tileH)
    {
        var c = center + new Vector2(0, tileH * 1.52f);
        var span = tileW * 1.40f;
        var phase = (float)((_anim * 0.09) % 1.0);

        for (var i = 0; i < 8; i++)
        {
            var t = (phase + i / 8f) % 1f;
            var x = Mathf.Lerp(-span, span, t);
            var y = x * (tileH / tileW);
            DrawCircle(c + new Vector2(x, y), 2.2f, i % 3 == 0 ? _gold : new Color(0.45f, 0.66f, 0.76f));
        }

        for (var i = 0; i < 7; i++)
        {
            var t = ((1f - phase) + i / 7f) % 1f;
            var x = Mathf.Lerp(span, -span, t);
            var y = -x * (tileH / tileW);
            DrawCircle(c + new Vector2(x, y), 2f, i % 2 == 0 ? _accent : new Color(0.55f, 0.63f, 0.70f));
        }
    }

    private void DrawBuilding(Vector2 basePoint, float width, float height, Color color, int seed)
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

        DrawColoredPolygon(leftFace, color.Darkened(0.34f));
        DrawColoredPolygon(rightFace, color.Darkened(0.22f));
        DrawColoredPolygon(topFace, color.Lightened(0.14f));

        if (height > 28)
        {
            var rows = Math.Min(4, (int)(height / 14));
            for (var r = 1; r <= rows; r++)
            {
                var y = top.Y + r * (height / (rows + 1));
                var window = ((seed + r) % 4 == 0)
                    ? new Color(0.97f, 0.84f, 0.53f)
                    : new Color(0.42f, 0.62f, 0.72f, 0.55f);
                DrawLine(new Vector2(basePoint.X - half + 2, y), new Vector2(basePoint.X - 1, y + 1.5f), window, 1.1f);
                DrawLine(new Vector2(basePoint.X + 1, y + 1.5f), new Vector2(basePoint.X + half - 2, y), window, 1.1f);
            }
        }
    }

    private void DrawTrees(Vector2 p, float tileW, float tileH, int districtId)
    {
        for (var i = 0; i < 7; i++)
        {
            var hash = StableHash($"{districtId}:{i}");
            var fx = ((hash & 0xFF) / 255f - 0.5f) * tileW * 0.55f;
            var fy = (((hash >> 8) & 0xFF) / 255f - 0.5f) * tileH * 0.42f;
            var tree = p + new Vector2(fx - fy, (fx + fy) * 0.44f);

            DrawLine(tree, tree - new Vector2(0, 7), new Color(0.29f, 0.23f, 0.17f), 2f);
            DrawCircle(tree - new Vector2(0, 10), 4f, new Color(0.18f, 0.49f, 0.40f));
            DrawCircle(tree - new Vector2(3, 8), 3f, new Color(0.14f, 0.37f, 0.31f));
        }
    }

    private void DrawVignette(Vector2 size)
    {
        var edge = new Color(0, 0, 0, 0.10f);
        DrawRect(new Rect2(0, 0, size.X, 8), edge, true);
        DrawRect(new Rect2(0, size.Y - 8, size.X, 8), edge, true);
        DrawRect(new Rect2(0, 0, 8, size.Y), edge, true);
        DrawRect(new Rect2(size.X - 8, 0, 8, size.Y), edge, true);
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
