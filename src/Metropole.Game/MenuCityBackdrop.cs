using Godot;

namespace Metropole.Game;

public partial class MenuCityBackdrop : Control
{
    private sealed class MovingNode
    {
        public required Node3D Node { get; init; }
        public required bool Horizontal { get; init; }
        public required float Lane { get; init; }
        public required float Phase { get; init; }
        public required float Speed { get; init; }
    }

    private readonly List<MovingNode> _traffic = [];
    private readonly List<MovingNode> _people = [];
    private Camera3D? _camera;
    private double _time;

    private static readonly string[] Buildings =
    [
        "res://assets/external/kenney/city/commercial/building-commercial-a.glb",
        "res://assets/external/kenney/city/commercial/building-c.glb",
        "res://assets/external/kenney/city/commercial/building-commercial-e.glb",
        "res://assets/external/kenney/city/commercial/building-g.glb",
        "res://assets/external/kenney/city/commercial/building-j.glb",
        "res://assets/external/kenney/city/commercial/building-n.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-a.glb",
        "res://assets/external/kenney/city/commercial/building-skyscraper-c.glb",
        "res://assets/external/kenney/city/industrial/building-a.glb",
        "res://assets/external/kenney/city/industrial/building-f.glb",
        "res://assets/external/kenney/city/industrial/building-industrial-m.glb",
        "res://assets/external/kaykit/city/building_A.gltf",
        "res://assets/external/kaykit/city/building_C.gltf",
        "res://assets/external/kaykit/city/building_E.gltf",
        "res://assets/external/kaykit/city/building_G.gltf"
    ];

    private static readonly string[] Vehicles =
    [
        "res://assets/external/kenney/vehicles/sedan.glb",
        "res://assets/external/kenney/vehicles/taxi.glb",
        "res://assets/external/kenney/vehicles/suv-luxury.glb",
        "res://assets/external/kenney/vehicles/hatchback-sports.glb",
        "res://assets/external/kenney/vehicles/delivery.glb",
        "res://assets/external/kenney/vehicles/ambulance.glb"
    ];

    private static readonly string[] People =
    [
        "res://assets/external/kenney/characters/female-a.glb",
        "res://assets/external/kenney/characters/female-b.glb",
        "res://assets/external/kenney/characters/male-a.glb",
        "res://assets/external/kenney/characters/male-b.glb"
    ];

    public int AssetCount { get; private set; }
    public int AnimatedCharacterCount { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcess(true);
        BuildScene();
    }

    public override void _Process(double delta)
    {
        _time += delta;

        if (_camera is not null)
        {
            var angle = 0.73 + Math.Sin(_time * 0.055) * 0.09;
            _camera.Position = new Vector3(
                (float)Math.Cos(angle) * 31f,
                21.5f + (float)Math.Sin(_time * 0.08) * 0.8f,
                (float)Math.Sin(angle) * 31f);
            _camera.LookAt(new Vector3(0, 1.6f, 0));
        }

        for (var i = 0; i < _traffic.Count; i++)
        {
            var item = _traffic[i];
            var phase = (float)((item.Phase + _time * item.Speed) % 1.0);
            var travel = Mathf.Lerp(-22f, 22f, phase);
            item.Node.Position = item.Horizontal
                ? new Vector3(travel, 0.18f, item.Lane)
                : new Vector3(item.Lane, 0.18f, travel);
            item.Node.Rotation = new Vector3(0, item.Horizontal ? -Mathf.Pi / 2f : 0, 0);
        }

        for (var i = 0; i < _people.Count; i++)
        {
            var item = _people[i];
            var phase = (float)((item.Phase + _time * item.Speed) % 1.0);
            var travel = Mathf.Lerp(-11f, 11f, phase);
            item.Node.Position = item.Horizontal
                ? new Vector3(travel, 0.12f, item.Lane)
                : new Vector3(item.Lane, 0.12f, travel);
            item.Node.Rotation = new Vector3(0, item.Horizontal ? -Mathf.Pi / 2f : 0, 0);
        }
    }

    private void BuildScene()
    {
        var container = new SubViewportContainer
        {
            Stretch = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(container);

        var viewport = new SubViewport
        {
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        container.AddChild(viewport);

        var root = new Node3D { Name = "MenuWorld" };
        viewport.AddChild(root);

        var panorama = GD.Load<Texture2D>("res://assets/external/polyhaven/hdri/urban_street_02_1k.hdr");
        var sky = panorama is null ? null : new Sky
        {
            SkyMaterial = new PanoramaSkyMaterial { Panorama = panorama }
        };

        var environment = new Godot.Environment
        {
            BackgroundMode = sky is null ? Godot.Environment.BGMode.Color : Godot.Environment.BGMode.Sky,
            BackgroundColor = new Color(0.025f, 0.055f, 0.085f),
            Sky = sky,
            AmbientLightSource = sky is null ? Godot.Environment.AmbientSource.Color : Godot.Environment.AmbientSource.Sky,
            AmbientLightColor = new Color(0.60f, 0.68f, 0.76f),
            AmbientLightEnergy = 0.74f,
            TonemapMode = Godot.Environment.ToneMapper.Agx
        };
        if (GraphicsQuality.RenderingMethod == "forward_plus")
        {
            environment.SsaoEnabled = true;
            environment.GlowEnabled = true;
            environment.SsrEnabled = true;
        }
        root.AddChild(new WorldEnvironment { Environment = environment });

        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-48f, -34f, 0),
            LightEnergy = 1.45f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 85f
        };
        root.AddChild(sun);

        _camera = new Camera3D
        {
            Current = true,
            Fov = 44f,
            Near = 0.1f,
            Far = 180f
        };
        root.AddChild(_camera);

        var ground = new MeshInstance3D
        {
            Mesh = CreateBox(new Vector3(50f, 0.30f, 50f), new Color(0.055f, 0.075f, 0.085f), 0.92f),
            Position = new Vector3(0, -0.25f, 0)
        };
        root.AddChild(ground);

        var roadMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.035f, 0.045f, 0.052f),
            Roughness = 0.72f
        };
        foreach (var axis in new[] { -8f, 0f, 8f })
        {
            var horizontal = new BoxMesh { Size = new Vector3(48f, 0.10f, 2.1f), Material = roadMat };
            root.AddChild(new MeshInstance3D { Mesh = horizontal, Position = new Vector3(0, 0.02f, axis) });
            var vertical = new BoxMesh { Size = new Vector3(2.1f, 0.10f, 48f), Material = roadMat };
            root.AddChild(new MeshInstance3D { Mesh = vertical, Position = new Vector3(axis, 0.025f, 0) });
        }

        for (var i = 0; i < Buildings.Length; i++)
        {
            var node = Instantiate(Buildings[i]);
            if (node is null) continue;
            var col = i % 5;
            var row = i / 5;
            var x = -16f + col * 8f;
            var z = -15f + row * 10f;
            if (Math.Abs(x) < 2.8f) x += 4f;
            node.Position = new Vector3(x, 0.18f, z);
            node.Scale = Vector3.One * (Buildings[i].Contains("kaykit", StringComparison.OrdinalIgnoreCase) ? 0.48f : 0.66f);
            node.Rotation = new Vector3(0, (i % 4) * Mathf.Pi / 2f, 0);
            root.AddChild(node);
            AssetCount++;
        }

        var propPaths = new[]
        {
            "res://assets/external/kaykit/city/streetlight.gltf",
            "res://assets/external/kaykit/city/trafficlight_A.gltf",
            "res://assets/external/kaykit/city/bench.gltf",
            "res://assets/external/kaykit/city/firehydrant.gltf",
            "res://assets/external/kaykit/city/bush.gltf"
        };
        for (var i = 0; i < 15; i++)
        {
            var node = Instantiate(propPaths[i % propPaths.Length]);
            if (node is null) continue;
            var ring = 7.2f + (i / 5) * 6.6f;
            var angle = i * Mathf.Tau / 5f + (i / 5) * 0.4f;
            node.Position = new Vector3(Mathf.Cos(angle) * ring, 0.12f, Mathf.Sin(angle) * ring);
            node.Scale = Vector3.One * 0.62f;
            node.Rotation = new Vector3(0, -angle, 0);
            root.AddChild(node);
            AssetCount++;
        }

        for (var i = 0; i < 12; i++)
        {
            var node = Instantiate(Vehicles[i % Vehicles.Length]);
            if (node is null) continue;
            node.Scale = Vector3.One * 0.64f;
            root.AddChild(node);
            _traffic.Add(new MovingNode
            {
                Node = node,
                Horizontal = (i & 1) == 0,
                Lane = ((i % 3) - 1) * 8f + ((i & 2) == 0 ? 0.62f : -0.62f),
                Phase = (i * 0.137f) % 1f,
                Speed = 0.022f + (i % 5) * 0.002f
            });
            AssetCount++;
        }

        for (var i = 0; i < 8; i++)
        {
            var node = Instantiate(People[i % People.Length]);
            if (node is null) continue;
            node.Scale = Vector3.One * 0.54f;
            root.AddChild(node);
            var animation = FindAnimationPlayer(node);
            if (animation is not null)
            {
                PlayAnimation(animation, i % 3 == 0 ? "idle" : "walk");
                AnimatedCharacterCount++;
            }
            _people.Add(new MovingNode
            {
                Node = node,
                Horizontal = (i & 1) == 0,
                Lane = ((i % 3) - 1) * 8f + 2.75f,
                Phase = (i * 0.173f) % 1f,
                Speed = 0.010f + (i % 4) * 0.001f
            });
            AssetCount++;
        }
    }

    private static BoxMesh CreateBox(Vector3 size, Color color, float roughness)
    {
        var mesh = new BoxMesh { Size = size };
        mesh.Material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = roughness
        };
        return mesh;
    }

    private static Node3D? Instantiate(string path)
    {
        var scene = GD.Load<PackedScene>(path);
        return scene?.Instantiate() as Node3D;
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

    private static void PlayAnimation(AnimationPlayer player, string token)
    {
        var animations = player.GetAnimationList()
            .Where(x => !x.ToString().Contains("RESET", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (animations.Length == 0) return;
        var chosen = animations[0];
        foreach (var candidate in animations)
        {
            if (!candidate.ToString().Contains(token, StringComparison.OrdinalIgnoreCase)) continue;
            chosen = candidate;
            break;
        }
        player.Play(chosen);
    }
}
