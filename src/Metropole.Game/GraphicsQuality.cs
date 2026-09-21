using Godot;

namespace Metropole.Game;

public enum GraphicsProfile
{
    Auto,
    Ultra,
    High,
    Balanced,
    Low
}

public readonly record struct GraphicsBudget(
    int RedrawHz,
    int Stars,
    int MaxCompaniesPerDistrict,
    int TreesPerDistrict,
    int MaxPedestriansPerDistrict,
    int ResidentPedestrianDivisor,
    float TrafficScale,
    int RainParticles,
    int WindowRows,
    bool DrawCityGrid,
    bool DrawStreetGlow,
    bool DrawBuildingDetails,
    bool DrawAmbientTrafficLights,
    bool DrawWeatherAtmosphere);

public sealed class AdaptiveGraphicsController
{
    private GraphicsProfile _requested = GraphicsProfile.Auto;
    private GraphicsProfile _effective = GraphicsProfile.Balanced;
    private double _sampleAccumulator;
    private int _lowFpsSamples;
    private int _highFpsSamples;
    private double _smoothedFps = 60.0;

    public GraphicsProfile Requested => _requested;
    public GraphicsProfile Effective => _requested == GraphicsProfile.Auto ? _effective : _requested;
    public double SmoothedFps => _smoothedFps;

    public GraphicsBudget Budget => BudgetFor(Effective);

    public void SetProfile(GraphicsProfile profile)
    {
        _requested = profile;
        if (profile != GraphicsProfile.Auto)
            _effective = profile;
        _lowFpsSamples = 0;
        _highFpsSamples = 0;
    }

    public bool Update(double delta)
    {
        _sampleAccumulator += delta;
        if (_sampleAccumulator < 1.0)
            return false;

        _sampleAccumulator = 0;
        var current = Math.Clamp(Engine.GetFramesPerSecond(), 1.0, 240.0);
        _smoothedFps = _smoothedFps * 0.72 + current * 0.28;

        if (_requested != GraphicsProfile.Auto)
            return false;

        if (_smoothedFps < 47.0)
        {
            _lowFpsSamples++;
            _highFpsSamples = 0;
        }
        else if (_smoothedFps > 58.0)
        {
            _highFpsSamples++;
            _lowFpsSamples = Math.Max(0, _lowFpsSamples - 1);
        }
        else
        {
            _lowFpsSamples = Math.Max(0, _lowFpsSamples - 1);
            _highFpsSamples = Math.Max(0, _highFpsSamples - 1);
        }

        if (_lowFpsSamples >= 4)
        {
            _lowFpsSamples = 0;
            _highFpsSamples = 0;
            var previous = _effective;
            _effective = _effective switch
            {
                GraphicsProfile.Ultra => GraphicsProfile.High,
                GraphicsProfile.High => GraphicsProfile.Balanced,
                GraphicsProfile.Balanced => GraphicsProfile.Low,
                _ => GraphicsProfile.Low
            };
            return previous != _effective;
        }

        if (_highFpsSamples >= 10)
        {
            _lowFpsSamples = 0;
            _highFpsSamples = 0;
            var previous = _effective;
            _effective = _effective switch
            {
                GraphicsProfile.Low => GraphicsProfile.Balanced,
                GraphicsProfile.Balanced => GraphicsProfile.High,
                GraphicsProfile.High => GraphicsProfile.Ultra,
                _ => GraphicsProfile.Ultra
            };
            return previous != _effective;
        }

        return false;
    }

    public string StatusText() =>
        $"{ProfileName(Requested)} → {ProfileName(Effective)} • {_smoothedFps:0} FPS";

    public static string ProfileName(GraphicsProfile profile) => profile switch
    {
        GraphicsProfile.Auto => "Automático",
        GraphicsProfile.Ultra => "Ultra",
        GraphicsProfile.High => "Alto",
        GraphicsProfile.Balanced => "Equilibrado",
        _ => "Leve"
    };

    public static GraphicsBudget BudgetFor(GraphicsProfile profile) => profile switch
    {
        GraphicsProfile.Ultra => new GraphicsBudget(
            RedrawHz: 60,
            Stars: 86,
            MaxCompaniesPerDistrict: 36,
            TreesPerDistrict: 13,
            MaxPedestriansPerDistrict: 20,
            ResidentPedestrianDivisor: 24,
            TrafficScale: 1.35f,
            RainParticles: 190,
            WindowRows: 7,
            DrawCityGrid: true,
            DrawStreetGlow: true,
            DrawBuildingDetails: true,
            DrawAmbientTrafficLights: true,
            DrawWeatherAtmosphere: true),

        GraphicsProfile.High => new GraphicsBudget(
            RedrawHz: 45,
            Stars: 64,
            MaxCompaniesPerDistrict: 32,
            TreesPerDistrict: 10,
            MaxPedestriansPerDistrict: 15,
            ResidentPedestrianDivisor: 30,
            TrafficScale: 1.10f,
            RainParticles: 135,
            WindowRows: 6,
            DrawCityGrid: true,
            DrawStreetGlow: true,
            DrawBuildingDetails: true,
            DrawAmbientTrafficLights: true,
            DrawWeatherAtmosphere: true),

        GraphicsProfile.Balanced => new GraphicsBudget(
            RedrawHz: 30,
            Stars: 46,
            MaxCompaniesPerDistrict: 28,
            TreesPerDistrict: 8,
            MaxPedestriansPerDistrict: 11,
            ResidentPedestrianDivisor: 38,
            TrafficScale: 0.88f,
            RainParticles: 85,
            WindowRows: 5,
            DrawCityGrid: true,
            DrawStreetGlow: true,
            DrawBuildingDetails: true,
            DrawAmbientTrafficLights: false,
            DrawWeatherAtmosphere: true),

        _ => new GraphicsBudget(
            RedrawHz: 20,
            Stars: 22,
            MaxCompaniesPerDistrict: 20,
            TreesPerDistrict: 5,
            MaxPedestriansPerDistrict: 6,
            ResidentPedestrianDivisor: 58,
            TrafficScale: 0.58f,
            RainParticles: 42,
            WindowRows: 3,
            DrawCityGrid: false,
            DrawStreetGlow: false,
            DrawBuildingDetails: false,
            DrawAmbientTrafficLights: false,
            DrawWeatherAtmosphere: false)
    };
}
