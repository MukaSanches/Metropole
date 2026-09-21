using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class CityPolishLayer : Node3D
{
    private sealed record StreetLamp(Node3D Root, MeshInstance3D Head, OmniLight3D Light);

    private SimulationEngine? _engine;
    private readonly List<StreetLamp> _lamps = [];
    private readonly List<MeshInstance3D> _plazaGlows = [];
    private StandardMaterial3D? _lampMaterial;
    private StandardMaterial3D? _plazaMaterial;

    public int StreetLightCount => _lamps.Count;
    public int ActiveLocalLightCount => _lamps.Count(x => x.Light.Visible && x.Light.LightEnergy > 0.02f);
    public bool Ready => _lamps.Count >= 12 && _lampMaterial is not null;

    public void SetEngine(SimulationEngine engine) => _engine = engine;

    public void Build()
    {
        if (_engine is null || _lamps.Count > 0) return;

        _lampMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.76f, 0.38f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.60f, 0.22f),
            EmissionEnergyMultiplier = 1.8f,
            Roughness = 0.32f
        };

        _plazaMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.16f, 0.54f, 0.58f),
            EmissionEnabled = true,
            Emission = new Color(0.10f, 0.56f, 0.62f),
            EmissionEnergyMultiplier = 0.42f,
            Roughness = 0.46f
        };

        BuildStreetLights();
        BuildDistrictAccents();
    }

    public void Update(GameState state, VisualQuality quality, double time)
    {
        if (_lampMaterial is null || _plazaMaterial is null) return;

        var daylight = Daylight(state.CurrentHour);
        var night = 1f - daylight;
        var storm = state.Weather.Contains("Chuva forte", StringComparison.OrdinalIgnoreCase);
        var fog = state.Weather.Contains("Neblina", StringComparison.OrdinalIgnoreCase);
        var pulse = 0.92f + MathF.Sin((float)time * 0.55f) * 0.08f;

        _lampMaterial.EmissionEnergyMultiplier = 0.35f + night * (storm || fog ? 3.2f : 2.4f) * pulse;
        _plazaMaterial.EmissionEnergyMultiplier = 0.10f + night * 0.82f;

        var allowedLights = quality switch
        {
            VisualQuality.Ultra => 12,
            VisualQuality.High => 8,
            VisualQuality.Medium => 4,
            _ => 0
        };

        for (var i = 0; i < _lamps.Count; i++)
        {
            var lamp = _lamps[i];
            lamp.Root.Visible = quality >= VisualQuality.Medium;
            lamp.Light.Visible = i < allowedLights && night > 0.14f;
            lamp.Light.LightEnergy = lamp.Light.Visible
                ? (0.15f + night * (storm ? 1.65f : 1.25f))
                : 0f;
            lamp.Light.LightColor = storm
                ? new Color(1.0f, 0.72f, 0.42f)
                : new Color(1.0f, 0.80f, 0.52f);
        }

        foreach (var glow in _plazaGlows)
            glow.Visible = quality >= VisualQuality.High && night > 0.10f;
    }

    public void ApplyQuality(VisualQuality quality)
    {
        foreach (var lamp in _lamps)
            lamp.Root.Visible = quality >= VisualQuality.Medium;

        foreach (var glow in _plazaGlows)
            glow.Visible = quality >= VisualQuality.High;
    }

    private void BuildStreetLights()
    {
        var positions = new List<Vector3>();
        for (var axis = -28f; axis <= 28f; axis += 14f)
        {
            positions.Add(new Vector3(axis + 2.2f, 0, -2.2f));
            positions.Add(new Vector3(axis - 2.2f, 0, 2.2f));
            positions.Add(new Vector3(-2.2f, 0, axis - 2.2f));
            positions.Add(new Vector3(2.2f, 0, axis + 2.2f));
        }

        foreach (var p in positions.Take(20))
        {
            var root = new Node3D { Position = p };

            var pole = new MeshInstance3D
            {
                Mesh = CreateBox(
                    new Vector3(0.10f, 2.6f, 0.10f),
                    new Color(0.12f, 0.14f, 0.16f),
                    0.44f),
                Position = new Vector3(0, 1.30f, 0)
            };
            root.AddChild(pole);

            var arm = new MeshInstance3D
            {
                Mesh = CreateBox(
                    new Vector3(0.72f, 0.08f, 0.08f),
                    new Color(0.13f, 0.15f, 0.17f),
                    0.42f),
                Position = new Vector3(0.30f, 2.53f, 0)
            };
            root.AddChild(arm);

            var head = new MeshInstance3D
            {
                Mesh = new BoxMesh
                {
                    Size = new Vector3(0.24f, 0.10f, 0.18f),
                    Material = _lampMaterial
                },
                Position = new Vector3(0.63f, 2.48f, 0)
            };
            root.AddChild(head);

            var light = new OmniLight3D
            {
                Position = new Vector3(0.63f, 2.34f, 0),
                OmniRange = 5.4f,
                LightEnergy = 0,
                LightColor = new Color(1.0f, 0.80f, 0.52f),
                ShadowEnabled = false,
                Visible = false
            };
            root.AddChild(light);

            AddChild(root);
            _lamps.Add(new StreetLamp(root, head, light));
        }
    }

    private void BuildDistrictAccents()
    {
        if (_engine is null || _plazaMaterial is null) return;

        foreach (var district in _engine.State.Districts)
        {
            var center = DistrictPosition(district);
            var accent = new MeshInstance3D
            {
                Mesh = new BoxMesh
                {
                    Size = new Vector3(2.3f, 0.025f, 0.10f),
                    Material = _plazaMaterial
                },
                Position = center + new Vector3(0, 0.23f, -5.05f)
            };
            AddChild(accent);
            _plazaGlows.Add(accent);
        }
    }

    private static Mesh CreateBox(Vector3 size, Color color, float roughness) =>
        new BoxMesh
        {
            Size = size,
            Material = new StandardMaterial3D
            {
                AlbedoColor = color,
                Roughness = roughness,
                Metallic = 0.05f
            }
        };

    private static Vector3 DistrictPosition(DistrictState district) =>
        new((district.GridX - 1) * 14f, 0, (district.GridY - 1) * 14f);

    private static float Daylight(int hour)
    {
        if (hour < 5 || hour >= 22) return 0.08f;
        if (hour < 7) return Mathf.Lerp(0.08f, 0.78f, (hour - 5) / 2f);
        if (hour < 18) return 1f;
        if (hour < 21) return Mathf.Lerp(1f, 0.14f, (hour - 18) / 3f);
        return 0.10f;
    }
}
