using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class ExternalAssetLayer : Node3D
{
    private sealed class VehicleProxy
    {
        public required Node3D Node { get; init; }
        public required bool Horizontal { get; init; }
        public required int LaneIndex { get; init; }
        public required float Direction { get; init; }
        public required float Speed { get; init; }
        public required float Phase { get; init; }
    }

    private sealed class PersonProxy
    {
        public required Node3D Node { get; init; }
        public required int DistrictIndex { get; init; }
        public required bool AlongX { get; init; }
        public required float Edge { get; init; }
        public required float Speed { get; init; }
        public required float Phase { get; init; }
        public AnimationPlayer? Animation { get; init; }
    }

    private SimulationEngine? _engine;
    private readonly List<Node3D> _buildings = [];
    private readonly List<VehicleProxy> _vehicles = [];
    private readonly List<PersonProxy> _people = [];
    private bool _built;

    private static readonly string[] CommercialBuildings =
    [
        "res://assets/external/kenney/city/building-commercial-a.glb",
        "res://assets/external/kenney/city/building-commercial-e.glb",
        "res://assets/external/kenney/city/building-commercial-h.glb",
        "res://assets/external/kenney/city/building-skyscraper-a.glb"
    ];

    private static readonly string[] IndustrialBuildings =
    [
        "res://assets/external/kenney/city/building-industrial-c.glb",
        "res://assets/external/kenney/city/building-industrial-m.glb"
    ];

    private static readonly string[] VehicleScenes =
    [
        "res://assets/external/kenney/vehicles/sedan.glb",
        "res://assets/external/kenney/vehicles/taxi.glb",
        "res://assets/external/kenney/vehicles/delivery.glb",
        "res://assets/external/kenney/vehicles/van.glb",
        "res://assets/external/kenney/vehicles/police.glb",
        "res://assets/external/kenney/vehicles/firetruck.glb"
    ];

    private static readonly string[] CharacterScenes =
    [
        "res://assets/external/kenney/characters/female-a.glb",
        "res://assets/external/kenney/characters/female-b.glb",
        "res://assets/external/kenney/characters/female-c.glb",
        "res://assets/external/kenney/characters/male-a.glb",
        "res://assets/external/kenney/characters/male-b.glb",
        "res://assets/external/kenney/characters/male-c.glb"
    ];

    public int DetailedAssetCount => _buildings.Count + _vehicles.Count + _people.Count;
    public int AnimatedProxyCount => _people.Count(p => p.Animation is not null);

    public void SetEngine(SimulationEngine engine) => _engine = engine;

    public void Build(VisualQuality quality)
    {
        if (_built || _engine is null) return;
        _built = true;

        BuildLandmarks();
        BuildDetailedVehicles();
        BuildAnimatedPeople();
        ApplyQuality(quality);
    }

    public void ApplyQuality(VisualQuality quality)
    {
        var buildingPerDistrict = quality switch
        {
            VisualQuality.Ultra => 3,
            VisualQuality.High => 2,
            VisualQuality.Medium => 1,
            _ => 0
        };

        for (var i = 0; i < _buildings.Count; i++)
            _buildings[i].Visible = (i % 3) < buildingPerDistrict;

        var vehicleCount = quality switch
        {
            VisualQuality.Ultra => 22,
            VisualQuality.High => 16,
            VisualQuality.Medium => 9,
            _ => 0
        };
        for (var i = 0; i < _vehicles.Count; i++)
            _vehicles[i].Node.Visible = i < vehicleCount;

        var peopleCount = quality switch
        {
            VisualQuality.Ultra => 30,
            VisualQuality.High => 20,
            VisualQuality.Medium => 10,
            _ => 0
        };
        for (var i = 0; i < _people.Count; i++)
            _people[i].Node.Visible = i < peopleCount;
    }

    public void Tick(double time, GameState state, VisualQuality quality)
    {
        if (!_built) return;

        var rainFactor = state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase) ? 0.76f : 1f;
        var rushFactor = state.CurrentHour is >= 7 and <= 9 or >= 16 and <= 19 ? 1.18f : 0.86f;

        foreach (var proxy in _vehicles)
        {
            if (!proxy.Node.Visible) continue;

            var phase = (float)((proxy.Phase + time * proxy.Speed * proxy.Direction * rushFactor) % 1.0);
            if (phase < 0) phase += 1f;
            var travel = Mathf.Lerp(-34f, 34f, phase);
            var laneAxis = (proxy.LaneIndex - 2) * 14f + (proxy.Direction > 0 ? 0.72f : -0.72f);

            proxy.Node.Position = proxy.Horizontal
                ? new Vector3(travel, 0.18f, laneAxis)
                : new Vector3(laneAxis, 0.18f, travel);
            proxy.Node.Rotation = new Vector3(0, proxy.Horizontal
                ? (proxy.Direction > 0 ? -Mathf.Pi / 2f : Mathf.Pi / 2f)
                : (proxy.Direction > 0 ? Mathf.Pi : 0f), 0);
        }

        var districts = state.Districts;
        foreach (var proxy in _people)
        {
            if (!proxy.Node.Visible || districts.Count == 0) continue;

            var district = districts[proxy.DistrictIndex % districts.Count];
            var center = DistrictPosition(district);
            var phase = (float)((proxy.Phase + time * proxy.Speed * rainFactor) % 1.0);
            var travel = Mathf.Lerp(-4.6f, 4.6f, phase);

            proxy.Node.Position = proxy.AlongX
                ? center + new Vector3(travel, 0.12f, proxy.Edge)
                : center + new Vector3(proxy.Edge, 0.12f, travel);
            proxy.Node.Rotation = new Vector3(0, proxy.AlongX ? -Mathf.Pi / 2f : 0f, 0);
        }

        ApplyQuality(quality);
    }

    private void BuildLandmarks()
    {
        if (_engine is null) return;
        var districts = _engine.State.Districts;

        for (var d = 0; d < districts.Count; d++)
        {
            var district = districts[d];
            var center = DistrictPosition(district);
            var industrial = district.LogisticsIndex > 1.12m;
            var pool = industrial ? IndustrialBuildings : CommercialBuildings;

            for (var i = 0; i < 3; i++)
            {
                var scenePath = pool[(d + i) % pool.Length];
                var node = InstantiateScene(scenePath);
                if (node is null) continue;

                var offset = i switch
                {
                    0 => new Vector3(-3.6f, 0.20f, -3.4f),
                    1 => new Vector3(3.5f, 0.20f, 3.2f),
                    _ => new Vector3(3.5f, 0.20f, -3.3f)
                };

                node.Position = center + offset;
                node.Scale = Vector3.One * (industrial ? 0.62f : 0.72f);
                node.Rotation = new Vector3(0, (d + i) % 4 * Mathf.Pi / 2f, 0);
                AddChild(node);
                _buildings.Add(node);
            }
        }
    }

    private void BuildDetailedVehicles()
    {
        const int count = 22;
        for (var i = 0; i < count; i++)
        {
            var node = InstantiateScene(VehicleScenes[i % VehicleScenes.Length]);
            if (node is null) continue;

            node.Scale = Vector3.One * 0.70f;
            AddChild(node);

            var hash = StableHash($"detail-car:{i}");
            _vehicles.Add(new VehicleProxy
            {
                Node = node,
                Horizontal = (hash & 1) == 0,
                LaneIndex = Math.Abs((hash >> 2) % 5),
                Direction = ((hash >> 5) & 1) == 0 ? 1f : -1f,
                Speed = 0.025f + Hash01(hash >> 8) * 0.018f,
                Phase = Hash01(hash >> 14)
            });
        }
    }

    private void BuildAnimatedPeople()
    {
        if (_engine is null) return;
        const int count = 30;

        for (var i = 0; i < count; i++)
        {
            var node = InstantiateScene(CharacterScenes[i % CharacterScenes.Length]);
            if (node is null) continue;

            node.Scale = Vector3.One * 0.58f;
            AddChild(node);

            var animation = FindAnimationPlayer(node);
            PlayBestAnimation(animation, "walk");

            var hash = StableHash($"detail-person:{i}");
            _people.Add(new PersonProxy
            {
                Node = node,
                DistrictIndex = i % Math.Max(1, _engine.State.Districts.Count),
                AlongX = (hash & 1) == 0,
                Edge = ((hash >> 2) & 1) == 0 ? 4.72f : -4.72f,
                Speed = 0.010f + Hash01(hash >> 7) * 0.008f,
                Phase = Hash01(hash >> 12),
                Animation = animation
            });
        }
    }

    private static Node3D? InstantiateScene(string path)
    {
        var packed = GD.Load<PackedScene>(path);
        if (packed is null)
        {
            GD.PushWarning($"External asset missing: {path}");
            return null;
        }

        var instance = packed.Instantiate();
        if (instance is Node3D node3D)
            return node3D;

        instance.QueueFree();
        GD.PushWarning($"External asset root is not Node3D: {path}");
        return null;
    }

    private static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player) return player;

        foreach (Node child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found is not null) return found;
        }
        return null;
    }

    private static void PlayBestAnimation(AnimationPlayer? player, string preferredToken)
    {
        if (player is null) return;

        var animations = player.GetAnimationList();
        if (animations.Length == 0) return;

        var selected = animations[0];
        var found = false;

        foreach (var name in animations)
        {
            if (!name.ToString().Contains(preferredToken, StringComparison.OrdinalIgnoreCase)) continue;
            selected = name;
            found = true;
            break;
        }

        if (!found)
        {
            foreach (var name in animations)
            {
                if (!name.ToString().Contains("idle", StringComparison.OrdinalIgnoreCase)) continue;
                selected = name;
                break;
            }
        }

        player.Play(selected);
    }

    private static Vector3 DistrictPosition(DistrictState district) =>
        new((district.GridX - 1) * 14f, 0, (district.GridY - 1) * 14f);

    private static int StableHash(string text)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in text) hash = hash * 31 + c;
            return hash & 0x7FFFFFFF;
        }
    }

    private static float Hash01(int value)
    {
        unchecked
        {
            var x = (uint)value;
            x ^= x >> 17;
            x *= 0xED5AD4BBu;
            x ^= x >> 11;
            x *= 0xAC4C1B51u;
            x ^= x >> 15;
            return (x & 0x00FFFFFF) / 16777215f;
        }
    }
}
