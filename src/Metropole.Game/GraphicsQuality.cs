using Godot;

namespace Metropole.Game;

public enum VisualQuality
{
    Low = 0,
    Medium = 1,
    High = 2,
    Ultra = 3
}

public static class GraphicsQuality
{
    public static string RenderingMethod => RenderingServer.GetCurrentRenderingMethod();
    public static string RenderingDriver => RenderingServer.GetCurrentRenderingDriverName();

    public static bool SupportsPremium3D =>
        !string.Equals(RenderingMethod, "gl_compatibility", StringComparison.OrdinalIgnoreCase);

    public static bool ForcePremium =>
        string.Equals(System.Environment.GetEnvironmentVariable("METROPOLE_FORCE_PREMIUM"), "1", StringComparison.Ordinal);

    public static bool ForceFallback =>
        string.Equals(System.Environment.GetEnvironmentVariable("METROPOLE_FORCE_FALLBACK"), "1", StringComparison.Ordinal);

    public static bool UsePremium3D => !ForceFallback && (ForcePremium || SupportsPremium3D);

    public static VisualQuality AutomaticCeiling()
    {
        return RenderingMethod switch
        {
            "forward_plus" => VisualQuality.High,
            "mobile" => VisualQuality.Medium,
            _ => VisualQuality.Low
        };
    }

    public static int BuildingsPerDistrict(VisualQuality quality) => quality switch
    {
        VisualQuality.Ultra => 34,
        VisualQuality.High => 28,
        VisualQuality.Medium => 20,
        _ => 12
    };

    public static int TreesPerDistrict(VisualQuality quality) => quality switch
    {
        VisualQuality.Ultra => 24,
        VisualQuality.High => 18,
        VisualQuality.Medium => 12,
        _ => 7
    };

    public static int VehicleCount(VisualQuality quality) => quality switch
    {
        VisualQuality.Ultra => 180,
        VisualQuality.High => 120,
        VisualQuality.Medium => 72,
        _ => 32
    };

    public static int PedestrianCount(VisualQuality quality) => quality switch
    {
        VisualQuality.Ultra => 360,
        VisualQuality.High => 220,
        VisualQuality.Medium => 120,
        _ => 48
    };

    public static float QualityRatio(VisualQuality quality) => quality switch
    {
        VisualQuality.Ultra => 1.0f,
        VisualQuality.High => 0.82f,
        VisualQuality.Medium => 0.58f,
        _ => 0.34f
    };
}
