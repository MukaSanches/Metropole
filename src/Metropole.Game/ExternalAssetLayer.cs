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
        public required float Phase { get; set; }
    }

    private sealed class PersonProxy
    {
        public required Node3D Node { get; init; }
        public required int CitizenId { get; init; }
        public required int DistrictIndex { get; init; }
        public required bool AlongX { get; init; }
        public required float Edge { get; init; }
        public required float Speed { get; init; }
        public required float Phase { get; set; }
        public AnimationPlayer? Animation { get; init; }
        public string LastActivity { get; set; } = "";
        public int LastAnimationBucket { get; set; } = -1;
    }

    private SimulationEngine? _engine;
    private readonly List<Node3D> _buildings = [];
    private readonly List<Node3D> _props = [];
    private readonly List<VehicleProxy> _vehicles = [];
    private readonly List<PersonProxy> _people = [];
    private readonly HashSet<string> _animationCatalog = new(StringComparer.OrdinalIgnoreCase);
    private bool _built;
    private double _lastTime = -1;
    private VisualQuality? _appliedQuality;
    private int _peopleBudget;
    private readonly List<Node3D> _shopDetails = [];


    private static readonly string[] CommercialBuildings =
    [
        "res://assets/external/kenney/city/commercial/building-commercial-a.glb",
        "res://assets/external/kenney/city/commercial/building-b.glb",
        "res://assets/external/kenney/city/commercial/building-c.glb",
        "res://assets/external/kenney/city/commercial/building-d.glb",
        "res://assets/external/kenney/city/commercial/building-commercial-e.glb",
        "res://assets/external/kenney/city/commercial/building-f.glb",
        "res://assets/external/kenney/city/commercial/building-g.glb",
        "res://assets/external/kenney/city/commercial/building-commercial-h.glb",
        "res://assets/external/kenney/city/commercial/building-i.glb",
        "res://assets/external/kenney/city/commercial/building-j.glb",
        "res://assets/external/kenney/city/commercial/building-k.glb",
        "res://assets/external/kenney/city/commercial/building-l.glb",
        "res://assets/external/kenney/city/commercial/building-m.glb",
        "res://assets/external/kenney/city/commercial/building-n.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-a.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-b.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-c.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-d.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-e.glb"
    ];

    private static readonly string[] IndustrialBuildings =
    [
        "res://assets/external/kenney/city/industrial/building-a.glb",
        "res://assets/external/kenney/city/industrial/building-b.glb",
        "res://assets/external/kenney/city/industrial/building-industrial-c.glb",
        "res://assets/external/kenney/city/industrial/building-d.glb",
        "res://assets/external/kenney/city/industrial/building-e.glb",
        "res://assets/external/kenney/city/industrial/building-f.glb",
        "res://assets/external/kenney/city/industrial/building-g.glb",
        "res://assets/external/kenney/city/industrial/building-h.glb",
        "res://assets/external/kenney/city/industrial/building-i.glb",
        "res://assets/external/kenney/city/industrial/building-j.glb",
        "res://assets/external/kenney/city/industrial/building-k.glb",
        "res://assets/external/kenney/city/industrial/building-l.glb",
        "res://assets/external/kenney/city/industrial/building-industrial-m.glb",
        "res://assets/external/kenney/city/industrial/building-n.glb",
        "res://assets/external/kenney/city/industrial/building-o.glb",
        "res://assets/external/kenney/city/industrial/building-p.glb",
        "res://assets/external/kenney/city/industrial/building-q.glb",
        "res://assets/external/kenney/city/industrial/building-r.glb",
        "res://assets/external/kenney/city/industrial/building-s.glb",
        "res://assets/external/kenney/city/industrial/building-t.glb"
    ];

    private static readonly string[] ResidentialBuildings =
    [
        "res://assets/external/kaykit/city/building_A.gltf",
        "res://assets/external/kaykit/city/building_B.gltf",
        "res://assets/external/kaykit/city/building_C.gltf",
        "res://assets/external/kaykit/city/building_D.gltf",
        "res://assets/external/kaykit/city/building_E.gltf",
        "res://assets/external/kaykit/city/building_F.gltf",
        "res://assets/external/kaykit/city/building_G.gltf",
        "res://assets/external/kaykit/city/building_H.gltf"
    ];

    private static readonly string[] UrbanProps =
    [
        "res://assets/external/kaykit/city/bench.gltf",
        "res://assets/external/kaykit/city/bush.gltf",
        "res://assets/external/kaykit/city/dumpster.gltf",
        "res://assets/external/kaykit/city/firehydrant.gltf",
        "res://assets/external/kaykit/city/streetlight.gltf",
        "res://assets/external/kaykit/city/trafficlight_A.gltf",
        "res://assets/external/kaykit/city/trafficlight_B.gltf",
        "res://assets/external/kaykit/city/trafficlight_C.gltf",
        "res://assets/external/kaykit/city/trash_A.gltf",
        "res://assets/external/kaykit/city/trash_B.gltf",
        "res://assets/external/kaykit/city/watertower.gltf"
    ];

    private static readonly string[] IndustrialProps =
    [
        "res://assets/external/kenney/factory/crane.glb",
        "res://assets/external/kenney/factory/crane-lift.glb",
        "res://assets/external/kenney/factory/machine.glb",
        "res://assets/external/kenney/factory/machine-fortified.glb",
        "res://assets/external/kenney/factory/robot-arm-a.glb",
        "res://assets/external/kenney/factory/robot-arm-b.glb",
        "res://assets/external/kenney/factory/conveyor.glb",
        "res://assets/external/kenney/factory/conveyor-corner.glb",
        "res://assets/external/kenney/factory/hopper-round.glb",
        "res://assets/external/kenney/factory/screen-panel-flat.glb",
        "res://assets/external/kenney/factory/pipe-large.glb",
        "res://assets/external/kenney/factory/pipe-large-curve.glb"
    ];

    private static readonly string[] VehicleScenes =
    [
        "res://assets/external/kenney/vehicles/sedan.glb",
        "res://assets/external/kenney/vehicles/taxi.glb",
        "res://assets/external/kenney/vehicles/delivery.glb",
        "res://assets/external/kenney/vehicles/van.glb",
        "res://assets/external/kenney/vehicles/police.glb",
        "res://assets/external/kenney/vehicles/firetruck.glb",
        "res://assets/external/kenney/vehicles/ambulance.glb",
        "res://assets/external/kenney/vehicles/garbage-truck.glb",
        "res://assets/external/kenney/vehicles/hatchback-sports.glb",
        "res://assets/external/kenney/vehicles/sedan-sports.glb",
        "res://assets/external/kenney/vehicles/suv-luxury.glb",
        "res://assets/external/kenney/vehicles/suv.glb",
        "res://assets/external/kenney/vehicles/truck-flat.glb",
        "res://assets/external/kenney/vehicles/truck.glb",
        "res://assets/external/kenney/vehicles/tractor.glb",
        "res://assets/external/kenney/vehicles/delivery-flat.glb"
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

    public event Action<int>? PersonSelected;

    public int DetailedAssetCount => _buildings.Count + _props.Count + _shopDetails.Count + _vehicles.Count + _people.Count;
    public int BuildingAssetCount => _buildings.Count;
    public int PropAssetCount => _props.Count + _shopDetails.Count;
    public int AnimatedProxyCount => _people.Count(p => p.Animation is not null);
    public int InteractivePersonCount => _people.Count;
    public int AvailableAnimationCount => _animationCatalog.Count;

    public bool TryGetCitizenPosition(int citizenId, out Vector3 position)
    {
        var proxy = _people.FirstOrDefault(p => p.CitizenId == citizenId);
        if (proxy is null || !proxy.Node.Visible)
        {
            position = Vector3.Zero;
            return false;
        }

        position = proxy.Node.GlobalPosition;
        return true;
    }

    public void SetEngine(SimulationEngine engine) => _engine = engine;

    public void Build(VisualQuality quality)
    {
        if (_built || _engine is null) return;
        _built = true;

        BuildLandmarks();
        BuildUrbanProps();
        BuildShopDetails();
        BuildPlazaFountains();
        BuildDetailedVehicles();
        BuildAnimatedPeople();
        ApplyQuality(quality);
    }

    public void ApplyQuality(VisualQuality quality)
    {
        if (_appliedQuality == quality) return;
        _appliedQuality = quality;
        var buildingsVisible = quality switch
        {
            VisualQuality.Ultra => _buildings.Count,
            VisualQuality.High => Math.Min(_buildings.Count, 60),
            VisualQuality.Medium => Math.Min(_buildings.Count, 34),
            _ => 0
        };
        for (var i = 0; i < _buildings.Count; i++)
            _buildings[i].Visible = i < buildingsVisible;

        var propsVisible = quality switch
        {
            VisualQuality.Ultra => _props.Count,
            VisualQuality.High => Math.Min(_props.Count, 120),
            VisualQuality.Medium => Math.Min(_props.Count, 58),
            _ => 0
        };
        for (var i = 0; i < _props.Count; i++)
            _props[i].Visible = i < propsVisible;

        var vehicleCount = quality switch
        {
            VisualQuality.Ultra => 64,
            VisualQuality.High => 46,
            VisualQuality.Medium => 24,
            _ => 0
        };
        for (var i = 0; i < _vehicles.Count; i++)
            _vehicles[i].Node.Visible = i < vehicleCount;

        var peopleCount = quality switch
        {
            VisualQuality.Ultra => 64,
            VisualQuality.High => 44,
            VisualQuality.Medium => 22,
            _ => 0
        };
        _peopleBudget = peopleCount;
        for (var i = 0; i < _shopDetails.Count; i++)
            _shopDetails[i].Visible = quality >= VisualQuality.High;

    }

    public void Tick(double time, GameState state, VisualQuality quality)
    {
        if (!_built) return;
        var delta = _lastTime < 0 ? 0 : Math.Clamp(time - _lastTime, 0, 0.1);
        _lastTime = time;
        ApplyQuality(quality);

        var rainFactor = state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase) ? 0.76f : 1f;
        var rushFactor = state.CurrentHour is >= 7 and <= 9 or >= 16 and <= 19 ? 1.18f : 0.86f;

        foreach (var proxy in _vehicles)
        {
            if (!proxy.Node.Visible) continue;

            proxy.Phase = (float)((proxy.Phase + delta * proxy.Speed * proxy.Direction * rushFactor * rainFactor) % 1.0);
            var phase = proxy.Phase;
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
        // Resolve only the bounded visual population; avoid allocating a full-city dictionary per tick.
        var animationBucket = (int)(time / 8.0);
        for (var i = 0; i < _people.Count; i++)
        {
            var proxy = _people[i];
            var citizen = i < _peopleBudget ? state.Citizens.Find(c => c.Id == proxy.CitizenId) : null;
            var activity = citizen?.CurrentActivity ?? "";
            var visible = citizen is { Alive: true } && districts.Count > 0 &&
                !activity.Contains("Dorm", StringComparison.OrdinalIgnoreCase);
            proxy.Node.Visible = visible;
            proxy.Node.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            if (!visible || citizen is null) continue;

            var district = districts.Find(d => d.Id == citizen.DistrictId) ?? districts[0];
            var moving = activity is "Deslocando-se" or "Exercitando-se" or "Lazer";
            var running = activity == "Exercitando-se";
            if (moving)
                proxy.Phase = (float)((proxy.Phase + delta * proxy.Speed * rainFactor * (running ? 1.6f : 1f)) % 1.0);
            var perimeter = proxy.Phase * 4f;
            var side = (int)perimeter;
            var travel = Mathf.Lerp(-4.72f, 4.72f, perimeter - side);
            var offset = side switch
            {
                0 => new Vector3(travel, 0.12f, -4.72f),
                1 => new Vector3(4.72f, 0.12f, travel),
                2 => new Vector3(-travel, 0.12f, 4.72f),
                _ => new Vector3(-4.72f, 0.12f, -travel)
            };
            proxy.Node.Position = DistrictPosition(district) + offset;
            var yaw = -Mathf.Pi / 2f - side * Mathf.Pi / 2f;
            proxy.Node.Rotation = new Vector3(0, Mathf.LerpAngle(proxy.Node.Rotation.Y, yaw,
                1f - MathF.Exp(-12f * (float)delta)), 0);
            if (proxy.Animation is not null)
                proxy.Animation.SpeedScale = moving ? (running ? 1.3f : 1f) : 0.8f;
            if (proxy.LastActivity == activity && proxy.LastAnimationBucket == animationBucket) continue;
            proxy.LastActivity = activity;
            proxy.LastAnimationBucket = animationBucket;
            PlayContextAnimation(proxy.Animation, moving ? (running ? "Exercitando-se" : "Deslocando-se") : activity,
                proxy.CitizenId + animationBucket);
        }
    }

    private void BuildPlazaFountains()
    {
        if (_engine is null) return;
        var stone = new StandardMaterial3D { AlbedoColor = new Color(0.43f, 0.47f, 0.49f), Roughness = 0.85f };
        var water = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/plaza_water.gdshader") };
        var basinMesh = new CylinderMesh { TopRadius = 1.05f, BottomRadius = 1.12f, Height = 0.24f, RadialSegments = 24 };
        var waterMesh = new CylinderMesh { TopRadius = 0.92f, BottomRadius = 0.92f, Height = 0.025f, RadialSegments = 24 };
        var pedestalMesh = new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.26f, Height = 0.65f, RadialSegments = 12 };
        foreach (var district in _engine.State.Districts)
        {
            if (district.LogisticsIndex > 1.12m) continue;
            var fountain = new Node3D { Name = $"PlazaFountain_{district.Id}", Position = DistrictPosition(district) };
            fountain.AddChild(new MeshInstance3D { Mesh = basinMesh, MaterialOverride = stone, Position = new Vector3(0, 0.22f, 0) });
            fountain.AddChild(new MeshInstance3D { Mesh = waterMesh, MaterialOverride = water, Position = new Vector3(0, 0.355f, 0) });
            fountain.AddChild(new MeshInstance3D { Mesh = pedestalMesh, MaterialOverride = stone, Position = new Vector3(0, 0.52f, 0) });
            AddChild(fountain);
            _shopDetails.Add(fountain);
        }
    }

    private void BuildShopDetails()
    {
        if (_engine is null) return;
        // Reuse the licensed 1.7 library: storefronts and terraces without additional textures.
        string[] names = ["detail-awning", "detail-awning-wide", "detail-parasol-a", "detail-parasol-b"];
        foreach (var district in _engine.State.Districts)
        {
            if (district.LogisticsIndex > 1.12m) continue;
            for (var i = 0; i < names.Length; i++)
            {
                var node = InstantiateScene($"res://assets/external/kenney/city/commercial/{names[i]}.glb");
                if (node is null) continue;
                node.Scale = Vector3.One * 0.62f;
                node.Position = DistrictPosition(district) + new Vector3(i < 2 ? -3.7f : 1.5f + (i - 2) * 1.5f,
                    i < 2 ? 1.1f : 0.2f, i < 2 ? -2.4f + i * 5.6f : -1.8f);
                node.Rotation = new Vector3(0, i == 1 ? Mathf.Pi : 0, 0);
                AddChild(node);
                _shopDetails.Add(node);
            }
        }
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
            var commercial = district.WealthIndex > 1.05m || d == 0;
            var pool = industrial ? IndustrialBuildings : commercial ? CommercialBuildings : ResidentialBuildings;

            for (var i = 0; i < 7; i++)
            {
                var scenePath = pool[(d * 7 + i) % pool.Length];
                var node = InstantiateScene(scenePath);
                if (node is null) continue;

                var offset = i switch
                {
                    0 => new Vector3(-3.7f, 0.20f, -3.5f),
                    1 => new Vector3(3.6f, 0.20f, 3.4f),
                    2 => new Vector3(3.5f, 0.20f, -3.4f),
                    3 => new Vector3(-3.6f, 0.20f, 3.3f),
                    4 => new Vector3(0.2f, 0.20f, 2.9f),
                    5 => new Vector3(-1.85f, 0.20f, 0.25f),
                    _ => new Vector3(1.95f, 0.20f, -0.35f)
                };

                node.Position = center + offset;
                node.Scale = Vector3.One * (industrial ? 0.54f : commercial ? 0.62f : 0.50f);
                node.Rotation = new Vector3(0, (d + i) % 4 * Mathf.Pi / 2f, 0);
                AddChild(node);
                _buildings.Add(node);
            }
        }
    }

    private void BuildUrbanProps()
    {
        if (_engine is null) return;

        foreach (var district in _engine.State.Districts)
        {
            var center = DistrictPosition(district);
            var industrial = district.LogisticsIndex > 1.12m;
            var pool = industrial ? IndustrialProps : UrbanProps;
            var count = industrial ? 14 : 18;

            for (var i = 0; i < count; i++)
            {
                var path = pool[(district.Id * 3 + i) % pool.Length];
                var node = InstantiateScene(path);
                if (node is null) continue;

                var hash = StableHash($"prop:{district.Id}:{i}");
                var angle = Hash01(hash) * Mathf.Tau;
                var radius = 3.2f + Hash01(hash >> 7) * 2.15f;
                node.Position = center + new Vector3(Mathf.Cos(angle) * radius, 0.16f, Mathf.Sin(angle) * radius);
                node.Rotation = new Vector3(0, Hash01(hash >> 11) * Mathf.Tau, 0);
                node.Scale = Vector3.One * (industrial ? 0.38f : 0.68f);
                AddChild(node);
                _props.Add(node);
            }
        }
    }

    private void BuildDetailedVehicles()
    {
        const int count = 64;
        for (var i = 0; i < count; i++)
        {
            var node = InstantiateScene(VehicleScenes[i % VehicleScenes.Length]);
            if (node is null) continue;

            node.Scale = Vector3.One * 0.66f;
            AddChild(node);

            var hash = StableHash($"detail-car:{i}");
            _vehicles.Add(new VehicleProxy
            {
                Node = node,
                Horizontal = (hash & 1) == 0,
                LaneIndex = Math.Abs((hash >> 2) % 5),
                Direction = ((hash >> 5) & 1) == 0 ? 1f : -1f,
                Speed = 0.024f + Hash01(hash >> 8) * 0.020f,
                Phase = Hash01(hash >> 14)
            });
        }
    }

    private void BuildAnimatedPeople()
    {
        if (_engine is null) return;

        var citizens = _engine.State.Citizens
            .Where(c => c.Alive && c.AgeYears >= 18)
            .OrderByDescending(c => c.IsPlayerPartner)
            .ThenByDescending(c => c.PlayerFamiliarity)
            .ThenBy(c => c.Id)
            .Take(64)
            .ToArray();

        for (var i = 0; i < citizens.Length; i++)
        {
            var citizen = citizens[i];
            var model = InstantiateScene(CharacterScenes[i % CharacterScenes.Length]);
            if (model is null) continue;

            var anchor = new Node3D { Name = $"Citizen_{citizen.Id}" };
            AddChild(anchor);

            model.Scale = Vector3.One * 0.56f;
            anchor.AddChild(model);

            var animation = FindAnimationPlayer(model);
            RegisterAnimations(animation);
            PlayContextAnimation(animation, citizen.CurrentActivity, citizen.Id);

            var area = new Area3D
            {
                Name = $"CitizenPick_{citizen.Id}",
                InputRayPickable = true,
                CollisionLayer = 1u << 7,
                CollisionMask = 0
            };
            var shape = new CollisionShape3D
            {
                Position = new Vector3(0, 0.95f, 0),
                Shape = new CapsuleShape3D
                {
                    Radius = 0.42f,
                    Height = 1.85f
                }
            };
            area.AddChild(shape);
            anchor.AddChild(area);

            var selectedId = citizen.Id;
            area.InputEvent += (Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx) =>
            {
                if (@event is InputEventMouseButton mouse &&
                    mouse.ButtonIndex == MouseButton.Left &&
                    mouse.Pressed && anchor.Visible)
                    PersonSelected?.Invoke(selectedId);
            };

            var hash = StableHash($"detail-person:{citizen.Id}");
            _people.Add(new PersonProxy
            {
                Node = anchor,
                CitizenId = citizen.Id,
                DistrictIndex = Math.Max(0, _engine.State.Districts.FindIndex(d => d.Id == citizen.DistrictId)),
                AlongX = (hash & 1) == 0,
                Edge = ((hash >> 2) & 1) == 0 ? 4.72f : -4.72f,
                Speed = 0.010f + Hash01(hash >> 7) * 0.008f,
                Phase = Hash01(hash >> 12),
                Animation = animation,
                LastActivity = citizen.CurrentActivity
            });
        }
    }

    private void RegisterAnimations(AnimationPlayer? player)
    {
        if (player is null) return;
        foreach (var name in player.GetAnimationList())
        {
            var value = name.ToString();
            if (!string.IsNullOrWhiteSpace(value) && !value.Contains("RESET", StringComparison.OrdinalIgnoreCase))
                _animationCatalog.Add(value);
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

    private static void PlayContextAnimation(AnimationPlayer? player, string activity, int selector)
    {
        if (player is null) return;
        var all = player.GetAnimationList()
            .Where(name => !name.ToString().Contains("RESET", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (all.Length == 0) return;

        var tokens = activity switch
        {
            "Dormindo" => new[] { "sleep", "lie", "idle" },
            "Trabalhando" => new[] { "work", "interact", "type", "idle" },
            "Estudando" => new[] { "sit", "read", "idle" },
            "Lazer" => new[] { "walk", "wave", "dance", "idle" },
            "Socializando" => new[] { "wave", "talk", "idle", "walk" },
            "Exercitando-se" => new[] { "run", "jog", "walk" },
            "Deslocando-se" => new[] { "walk", "jog", "run" },
            _ => new[] { "idle", "walk" }
        };

        // Respect priority: walking must not randomly select idle and slide along the street.
        var pool = all;
        foreach (var token in tokens)
        {
            var candidates = all.Where(name => name.ToString().Contains(token, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (candidates.Length == 0) continue;
            pool = candidates;
            break;
        }
        var selected = pool[Math.Abs(selector) % pool.Length];
        if (player.CurrentAnimation != selected)
        {
            var clip = player.GetAnimation(selected);
            if (clip is not null) clip.LoopMode = Animation.LoopModeEnum.Linear;
            player.Play(selected, 0.22);
        }
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
