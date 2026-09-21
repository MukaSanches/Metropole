using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class CityView : Control
{
    private SimulationEngine? _engine;

    public void SetEngine(SimulationEngine engine)
    {
        _engine = engine;
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }

    public override void _Draw()
    {
        if (_engine is null) return;

        var state = _engine.State;
        var size = Size;
        var center = new Vector2(size.X * 0.50f, size.Y * 0.43f);
        const float tileW = 220f;
        const float tileH = 112f;

        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.035f, 0.05f, 0.075f), true);

        foreach (var district in state.Districts.OrderBy(d => d.GridX + d.GridY))
        {
            var isoX = (district.GridX - district.GridY) * tileW * 0.52f;
            var isoY = (district.GridX + district.GridY) * tileH * 0.52f;
            var p = center + new Vector2(isoX, isoY - 120f);
            var points = new[]
            {
                p + new Vector2(0, -tileH / 2),
                p + new Vector2(tileW / 2, 0),
                p + new Vector2(0, tileH / 2),
                p + new Vector2(-tileW / 2, 0)
            };

            var wealth = (float)Math.Clamp(district.WealthIndex, 0.7m, 1.6m);
            var baseColor = district.Id == state.Player.DistrictId
                ? new Color(0.14f, 0.42f, 0.52f)
                : new Color(0.08f + wealth * 0.035f, 0.17f + wealth * 0.04f, 0.22f + wealth * 0.035f);
            DrawColoredPolygon(points, baseColor);
            for (var i = 0; i < points.Length; i++)
                DrawLine(points[i], points[(i + 1) % points.Length], new Color(0.22f, 0.37f, 0.46f), 1.3f);

            var companies = state.Companies
                .Where(c => c.Open && c.DistrictId == district.Id)
                .OrderBy(c => c.Id)
                .Take(18)
                .ToArray();

            for (var i = 0; i < companies.Length; i++)
            {
                var row = i / 6;
                var col = i % 6;
                var bx = (col - 2.5f) * 25f;
                var by = (row - 1f) * 18f;
                var footprint = p + new Vector2(bx - by, (bx + by) * 0.42f);
                var h = 10f + (companies[i].Id % 5) * 5f;
                var top = footprint - new Vector2(0, h);
                var bColor = companies[i].PlayerOwned
                    ? new Color(0.93f, 0.67f, 0.24f)
                    : SectorColor(companies[i].Sector);
                DrawRect(new Rect2(top.X - 6f, top.Y - 4f, 12f, h + 8f), bColor, true);
                DrawLine(new Vector2(top.X - 6f, top.Y - 4f), new Vector2(top.X - 6f, footprint.Y + 4f), bColor.Lightened(0.18f), 1f);
            }
        }

        var roads = new Color(0.18f, 0.25f, 0.29f, 0.85f);
        DrawLine(center + new Vector2(-330, 25), center + new Vector2(330, 355), roads, 5f);
        DrawLine(center + new Vector2(330, 25), center + new Vector2(-330, 355), roads, 5f);
    }

    private static Color SectorColor(string sector)
    {
        var hash = StableHash(sector);
        var r = 0.25f + ((hash & 0xFF) / 255f) * 0.35f;
        var g = 0.32f + (((hash >> 8) & 0xFF) / 255f) * 0.35f;
        var b = 0.40f + (((hash >> 16) & 0xFF) / 255f) * 0.35f;
        return new Color(r, g, b);
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
