using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class FinanceChart : Control
{
    private CompanyState? _company;

    public void SetCompany(CompanyState company)
    {
        _company = company;
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        if (size.X < 20 || size.Y < 20) return;

        var bg = new Color(0.045f, 0.075f, 0.105f);
        var grid = new Color(0.14f, 0.23f, 0.31f, 0.45f);
        var positive = new Color(0.43f, 0.86f, 0.56f);
        var negative = new Color(1.0f, 0.42f, 0.42f);
        var revenueColor = new Color(0.24f, 0.84f, 0.82f, 0.48f);

        DrawRect(new Rect2(Vector2.Zero, size), bg, true);

        var pad = 12f;
        var rect = new Rect2(pad, pad, MathF.Max(1, size.X - pad * 2), MathF.Max(1, size.Y - pad * 2));
        for (var i = 1; i < 4; i++)
        {
            var y = rect.Position.Y + rect.Size.Y * i / 4f;
            DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.End.X, y), grid, 1f);
        }

        if (_company is null || _company.FinanceHistory.Count < 2) return;

        var history = _company.FinanceHistory.TakeLast(45).ToArray();
        var maxAbsProfit = history.Max(h => Math.Abs(h.Profit));
        var maxRevenue = history.Max(h => h.Revenue);
        if (maxAbsProfit < 1m) maxAbsProfit = 1m;
        if (maxRevenue < 1m) maxRevenue = 1m;

        var midY = rect.Position.Y + rect.Size.Y * 0.58f;
        DrawLine(new Vector2(rect.Position.X, midY), new Vector2(rect.End.X, midY), new Color(0.45f, 0.55f, 0.62f, 0.58f), 1f);

        Vector2? previousProfit = null;
        Vector2? previousRevenue = null;

        for (var i = 0; i < history.Length; i++)
        {
            var x = rect.Position.X + rect.Size.X * i / Math.Max(1, history.Length - 1);
            var profitNorm = (float)(history[i].Profit / maxAbsProfit);
            var profitY = midY - profitNorm * rect.Size.Y * 0.34f;
            var profitPoint = new Vector2(x, profitY);

            var revenueNorm = (float)(history[i].Revenue / maxRevenue);
            var revenueY = rect.End.Y - revenueNorm * rect.Size.Y * 0.25f;
            var revenuePoint = new Vector2(x, revenueY);

            if (previousRevenue is Vector2 prevRevenue)
                DrawLine(prevRevenue, revenuePoint, revenueColor, 1.6f, true);

            if (previousProfit is Vector2 prevProfit)
                DrawLine(prevProfit, profitPoint, history[i].Profit >= 0 ? positive : negative, 2.2f, true);

            previousProfit = profitPoint;
            previousRevenue = revenuePoint;
        }
    }
}
