using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class PremiumCityView : Control
{
    private SimulationEngine? _engine;
    private SubViewportContainer? _viewportContainer;
    private SubViewport? _viewport;
    private Node3D? _worldRoot;
    private Camera3D? _camera;
    private DirectionalLight3D? _sun;
    private WorldEnvironment? _worldEnvironment;
    private Godot.Environment? _environment;
    private CityWeatherOverlay? _weatherOverlay;
    private ExternalAssetLayer? _externalAssets;

    private readonly List<(MultiMeshInstance3D Node, int FullCount)> _scalableGroups = [];
    private MultiMeshInstance3D? _vehicles;
    private MultiMeshInstance3D? _pedestrians;

    private VisualQuality _quality;
    private VisualQuality _ceiling;
    private VisualQuality? _manualQuality;
    private double _anim;
    private double _visualAccumulator;
    private double _performanceWindow;
    private double _frameTimeSum;
    private int _frameSamples;
    private double _upgradeWindow;

    private Vector3 _cameraTarget = Vector3.Zero;
    private float _cameraSize = 47f;
    private bool _dragging;
    private Vector2 _lastMouse;
    private int _lastBuiltDay = -1;
    private int _lastOpenCompanies = -1;

    public string Diagnostics =>
        $"{GraphicsQuality.RenderingMethod}/{GraphicsQuality.RenderingDriver} • {QualityModeLabel} • 3D • assets {_externalAssets?.DetailedAssetCount ?? 0}";

    public int DetailedAssetCount => _externalAssets?.DetailedAssetCount ?? 0;
    public int AnimatedProxyCount => _externalAssets?.AnimatedProxyCount ?? 0;

    public string QualityModeLabel =>
        _manualQuality is VisualQuality fixedQuality
            ? fixedQuality.ToString().ToUpperInvariant()
            : $"AUTO/{_quality.ToString().ToUpperInvariant()}";

    public void CycleQualityMode()
    {
        _manualQuality = _manualQuality switch
        {
            null => VisualQuality.Ultra,
            VisualQuality.Ultra => VisualQuality.High,
            VisualQuality.High => VisualQuality.Medium,
            VisualQuality.Medium => VisualQuality.Low,
            _ => null
        };

        _quality = _manualQuality ?? _ceiling;
        _upgradeWindow = 0;
        ApplyQualityFeatures();
        RebuildWorld();
    }

    public void SetEngine(SimulationEngine engine)
    {
        _engine = engine;
        _weatherOverlay?.SetEngine(engine);
        if (IsNodeReady())
            RebuildWorld();
    }

    public void RefreshFromSimulation()
    {
        if (_engine is null) return;

        var state = _engine.State;
        var structuralChange =
            _lastOpenCompanies != state.OpenCompanies ||
            _lastBuiltDay < 0 ||
            state.CurrentDay - _lastBuiltDay >= 7;

        if (structuralChange)
            RebuildWorld();
        else
            UpdateAtmosphere();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SetProcess(true);

        _ceiling = GraphicsQuality.AutomaticCeiling();
        _quality = _ceiling;

        BuildViewport();
        if (_engine is not null)
            RebuildWorld();
    }

    public override void _Process(double delta)
    {
        _anim += delta;
        _visualAccumulator += delta;
        TrackAdaptiveQuality(delta);

        if (_visualAccumulator >= 1.0 / 24.0)
        {
            _visualAccumulator = 0;
            UpdateAtmosphere();
            UpdateVehicles();
            UpdatePedestrians();
            if (_engine is not null)
                _externalAssets?.Tick(_anim, _engine.State, _quality);
            _weatherOverlay?.SetAnimationTime(_anim);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.WheelUp && button.Pressed)
            {
                _cameraSize = Math.Max(24f, _cameraSize - 3.2f);
                ApplyCamera();
                AcceptEvent();
            }
            else if (button.ButtonIndex == MouseButton.WheelDown && button.Pressed)
            {
                _cameraSize = Math.Min(78f, _cameraSize + 3.2f);
                ApplyCamera();
                AcceptEvent();
            }
            else if (button.ButtonIndex is MouseButton.Middle or MouseButton.Right)
            {
                _dragging = button.Pressed;
                _lastMouse = button.Position;
                AcceptEvent();
            }
        }
        else if (@event is InputEventMouseMotion motion && _dragging)
        {
            var delta = motion.Position - _lastMouse;
            _lastMouse = motion.Position;
            var scale = _cameraSize / 900f;
            _cameraTarget += new Vector3(-delta.X * scale, 0, -delta.Y * scale);
            _cameraTarget.X = Math.Clamp(_cameraTarget.X, -20f, 20f);
            _cameraTarget.Z = Math.Clamp(_cameraTarget.Z, -20f, 20f);
            ApplyCamera();
            AcceptEvent();
        }
    }

    private void BuildViewport()
    {
        _viewportContainer = new SubViewportContainer
        {
            Stretch = true,
            MouseFilter = MouseFilterEnum.Pass
        };
        _viewportContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_viewportContainer);

        _viewport = new SubViewport
        {
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        _viewportContainer.AddChild(_viewport);

        _worldRoot = new Node3D { Name = "World3D" };
        _viewport.AddChild(_worldRoot);

        _environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.035f, 0.09f, 0.13f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.44f, 0.55f, 0.62f),
            AmbientLightEnergy = 0.78f,
            TonemapMode = Godot.Environment.ToneMapper.Agx
        };

        _worldEnvironment = new WorldEnvironment
        {
            Environment = _environment
        };
        _worldRoot.AddChild(_worldEnvironment);

        _sun = new DirectionalLight3D
        {
            LightEnergy = 1.25f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 95f
        };
        _worldRoot.AddChild(_sun);

        _camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = _cameraSize,
            Near = 0.1f,
            Far = 250f,
            Current = true
        };
        _worldRoot.AddChild(_camera);
        ApplyCamera();

        _weatherOverlay = new CityWeatherOverlay
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _weatherOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_weatherOverlay);
        if (_engine is not null)
            _weatherOverlay.SetEngine(_engine);

        ApplyQualityFeatures();
    }

    private void RebuildWorld()
    {
        if (_engine is null || _worldRoot is null) return;

        foreach (var child in _worldRoot.GetChildren())
        {
            if (child == _worldEnvironment || child == _sun || child == _camera) continue;
            _worldRoot.RemoveChild(child);
            child.QueueFree();
        }

        _scalableGroups.Clear();
        _vehicles = null;
        _pedestrians = null;
        _externalAssets = null;

        BuildGround();
        BuildRoadNetwork();
        BuildDistricts();
        BuildVehicles();
        BuildPedestrians();
        BuildExternalAssets();
        _lastBuiltDay = _engine.State.CurrentDay;
        _lastOpenCompanies = _engine.State.OpenCompanies;
        UpdateAtmosphere();
        ApplyQualityFeatures();
    }

    private void BuildGround()
    {
        if (_worldRoot is null) return;

        var floor = new MeshInstance3D
        {
            Mesh = CreateBoxMesh(new Vector3(74f, 0.35f, 74f), new Color(0.045f, 0.105f, 0.105f), 0.94f)
        };
        floor.Position = new Vector3(0, -0.35f, 0);
        _worldRoot.AddChild(floor);

        var water = new MeshInstance3D
        {
            Mesh = CreateBoxMesh(new Vector3(78f, 0.05f, 78f), new Color(0.025f, 0.070f, 0.082f), 0.72f)
        };
        water.Position = new Vector3(0, -0.58f, 0);
        _worldRoot.AddChild(water);
    }

    private void BuildRoadNetwork()
    {
        if (_worldRoot is null) return;

        var asphalt = new Color(0.055f, 0.065f, 0.075f);
        var sidewalk = new Color(0.18f, 0.20f, 0.21f);

        for (var i = -2; i <= 2; i++)
        {
            var axis = i * 14f;
            AddRoad(new Vector3(0, 0.02f, axis), new Vector3(70f, 0.20f, 2.4f), asphalt);
            AddRoad(new Vector3(axis, 0.025f, 0), new Vector3(2.4f, 0.20f, 70f), asphalt);

            AddRoad(new Vector3(0, 0.08f, axis - 1.65f), new Vector3(70f, 0.12f, 0.48f), sidewalk);
            AddRoad(new Vector3(0, 0.08f, axis + 1.65f), new Vector3(70f, 0.12f, 0.48f), sidewalk);
            AddRoad(new Vector3(axis - 1.65f, 0.08f, 0), new Vector3(0.48f, 0.12f, 70f), sidewalk);
            AddRoad(new Vector3(axis + 1.65f, 0.08f, 0), new Vector3(0.48f, 0.12f, 70f), sidewalk);
        }

        var lane = new Color(0.80f, 0.68f, 0.32f);
        for (var i = -2; i <= 2; i++)
        {
            var axis = i * 14f;
            AddRoad(new Vector3(0, 0.145f, axis), new Vector3(70f, 0.025f, 0.055f), lane);
            AddRoad(new Vector3(axis, 0.15f, 0), new Vector3(0.055f, 0.025f, 70f), lane);
        }
    }

    private void AddRoad(Vector3 position, Vector3 size, Color color)
    {
        if (_worldRoot is null) return;
        var mesh = new MeshInstance3D
        {
            Mesh = CreateBoxMesh(size, color, 0.86f),
            Position = position
        };
        _worldRoot.AddChild(mesh);
    }

    private void BuildDistricts()
    {
        if (_engine is null || _worldRoot is null) return;

        var state = _engine.State;
        foreach (var district in state.Districts)
        {
            var center = DistrictPosition(district);
            var districtColor = DistrictColor(district);

            var pad = new MeshInstance3D
            {
                Mesh = CreateBoxMesh(new Vector3(10.8f, 0.22f, 10.8f), districtColor, 0.91f),
                Position = center + new Vector3(0, 0.08f, 0)
            };
            _worldRoot.AddChild(pad);

            BuildDistrictBuildings(district, center);
            BuildDistrictTrees(district, center);
        }
    }

    private void BuildDistrictBuildings(DistrictState district, Vector3 center)
    {
        if (_engine is null || _worldRoot is null) return;

        var state = _engine.State;
        var companies = state.Companies
            .Where(c => c.Open && c.DistrictId == district.Id)
            .OrderBy(c => c.Id)
            .ToArray();

        var count = GraphicsQuality.BuildingsPerDistrict(VisualQuality.Ultra);
        var box = new BoxMesh { Size = Vector3.One };
        box.Material = CreateVertexColorMaterial(0.68f, 0.08f);

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = box,
            InstanceCount = count,
            VisibleInstanceCount = count
        };

        for (var i = 0; i < count; i++)
        {
            var company = i < companies.Length ? companies[i] : null;
            var h = StableHash($"building:{district.Id}:{i}");
            var col = i % 6;
            var row = i / 6;
            var x = -4.25f + col * 1.72f + Hash01(h) * 0.18f;
            var z = -4.20f + row * 1.85f + Hash01(h >> 7) * 0.18f;

            if (Math.Abs(x) < 0.85f) x += x < 0 ? -1.0f : 1.0f;
            if (Math.Abs(z) < 0.85f) z += z < 0 ? -1.0f : 1.0f;

            var wealth = (float)district.WealthIndex;
            var height = company is null
                ? 1.2f + Hash01(h >> 2) * (3.5f + wealth * 2.3f)
                : 2.4f + (float)company.Reputation * 5.2f + (float)company.BrandAwareness * 2.2f;

            var width = 0.85f + Hash01(h >> 11) * 0.55f;
            var depth = 0.85f + Hash01(h >> 15) * 0.55f;
            if (company?.PlayerOwned == true)
            {
                width *= 1.35f;
                depth *= 1.35f;
                height *= 1.20f;
            }

            var position = center + new Vector3(x, 0.25f + height * 0.5f, z);
            var basis = ScaledBasis(new Vector3(width, height, depth));
            multi.SetInstanceTransform(i, new Transform3D(basis, position));

            var color = company is null
                ? ResidentialColor(h, district)
                : CompanyColor(company);

            multi.SetInstanceColor(i, color);
        }

        var node = new MultiMeshInstance3D
        {
            Multimesh = multi,
            Name = $"Buildings_{district.Id}"
        };
        _worldRoot.AddChild(node);
        _scalableGroups.Add((node, count));
    }

    private void BuildDistrictTrees(DistrictState district, Vector3 center)
    {
        if (_worldRoot is null) return;

        var count = GraphicsQuality.TreesPerDistrict(VisualQuality.Ultra);

        var crownMesh = new SphereMesh
        {
            Radius = 0.36f,
            Height = 0.72f,
            RadialSegments = 8,
            Rings = 4
        };
        crownMesh.Material = CreateVertexColorMaterial(0.88f, 0.0f);

        var crowns = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = crownMesh,
            InstanceCount = count,
            VisibleInstanceCount = count
        };

        var trunkMesh = new BoxMesh { Size = Vector3.One };
        trunkMesh.Material = CreateVertexColorMaterial(0.96f, 0.0f);

        var trunks = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = trunkMesh,
            InstanceCount = count,
            VisibleInstanceCount = count
        };

        for (var i = 0; i < count; i++)
        {
            var h = StableHash($"tree:{district.Id}:{i}");
            var x = -5.0f + Hash01(h) * 10f;
            var z = -5.0f + Hash01(h >> 8) * 10f;

            if (Math.Abs(x) < 2.0f && Math.Abs(z) < 2.0f)
                x = Math.Sign(x == 0 ? 1 : x) * (3.5f + Hash01(h >> 12));

            var p = center + new Vector3(x, 0, z);
            crowns.SetInstanceTransform(i, new Transform3D(ScaledBasis(new Vector3(0.85f, 1.0f, 0.85f)), p + new Vector3(0, 1.05f, 0)));
            crowns.SetInstanceColor(i, new Color(0.12f + Hash01(h >> 2) * 0.05f, 0.35f + Hash01(h >> 4) * 0.15f, 0.20f + Hash01(h >> 6) * 0.08f));

            trunks.SetInstanceTransform(i, new Transform3D(ScaledBasis(new Vector3(0.14f, 0.70f, 0.14f)), p + new Vector3(0, 0.35f, 0)));
            trunks.SetInstanceColor(i, new Color(0.30f, 0.22f, 0.13f));
        }

        var crownNode = new MultiMeshInstance3D { Multimesh = crowns, Name = $"Trees_{district.Id}" };
        var trunkNode = new MultiMeshInstance3D { Multimesh = trunks, Name = $"Trunks_{district.Id}" };
        _worldRoot.AddChild(crownNode);
        _worldRoot.AddChild(trunkNode);
        _scalableGroups.Add((crownNode, count));
        _scalableGroups.Add((trunkNode, count));
    }

    private void BuildExternalAssets()
    {
        if (_engine is null || _worldRoot is null) return;

        _externalAssets = new ExternalAssetLayer { Name = "ExternalCC0Assets" };
        _externalAssets.SetEngine(_engine);
        _worldRoot.AddChild(_externalAssets);
        _externalAssets.Build(_quality);
    }

    private void BuildVehicles()
    {
        if (_worldRoot is null) return;

        var count = GraphicsQuality.VehicleCount(VisualQuality.Ultra);
        var mesh = new BoxMesh { Size = Vector3.One };
        mesh.Material = CreateVertexColorMaterial(0.40f, 0.22f);

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = count,
            VisibleInstanceCount = count
        };

        for (var i = 0; i < count; i++)
        {
            multi.SetInstanceColor(i, VehicleColor(i));
            multi.SetInstanceTransform(i, Transform3D.Identity);
        }

        _vehicles = new MultiMeshInstance3D { Multimesh = multi, Name = "Traffic" };
        _worldRoot.AddChild(_vehicles);
        _scalableGroups.Add((_vehicles, count));
    }

    private void BuildPedestrians()
    {
        if (_worldRoot is null) return;

        var count = GraphicsQuality.PedestrianCount(VisualQuality.Ultra);
        var mesh = new BoxMesh { Size = Vector3.One };
        mesh.Material = CreateVertexColorMaterial(0.82f, 0.0f);

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = count,
            VisibleInstanceCount = count
        };

        for (var i = 0; i < count; i++)
        {
            multi.SetInstanceColor(i, PersonColor(i));
            multi.SetInstanceTransform(i, Transform3D.Identity);
        }

        _pedestrians = new MultiMeshInstance3D { Multimesh = multi, Name = "Pedestrians" };
        _worldRoot.AddChild(_pedestrians);
        _scalableGroups.Add((_pedestrians, count));
    }

    private void UpdateVehicles()
    {
        if (_vehicles?.Multimesh is not MultiMesh multi || _engine is null) return;

        var state = _engine.State;
        var rush = state.CurrentHour is >= 7 and <= 9 or >= 16 and <= 19 ? 1.0f :
                   state.CurrentHour is >= 0 and <= 5 ? 0.38f : 0.72f;
        var visible = Math.Max(8, (int)(multi.InstanceCount * GraphicsQuality.QualityRatio(_quality) * rush));
        multi.VisibleInstanceCount = Math.Min(visible, multi.InstanceCount);

        for (var i = 0; i < multi.VisibleInstanceCount; i++)
        {
            var horizontal = (i & 1) == 0;
            var laneIndex = (i / 2) % 5;
            var laneAxis = (laneIndex - 2) * 14f + ((i % 4) < 2 ? 0.72f : -0.72f);
            var direction = ((i / 10) & 1) == 0 ? 1f : -1f;
            var phase = (float)((_anim * (0.030 + (i % 7) * 0.0018) * direction + i * 0.071) % 1.0);
            if (phase < 0) phase += 1f;
            var travel = Mathf.Lerp(-34f, 34f, phase);

            var pos = horizontal
                ? new Vector3(travel, 0.38f, laneAxis)
                : new Vector3(laneAxis, 0.38f, travel);

            var scale = horizontal
                ? new Vector3(0.85f, 0.34f, 0.40f)
                : new Vector3(0.40f, 0.34f, 0.85f);

            multi.SetInstanceTransform(i, new Transform3D(ScaledBasis(scale), pos));
        }
    }

    private void UpdatePedestrians()
    {
        if (_pedestrians?.Multimesh is not MultiMesh multi || _engine is null) return;

        var state = _engine.State;
        var activity = state.CurrentHour is >= 7 and <= 21 ? 1f : 0.28f;
        if (state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase))
            activity *= 0.62f;

        var visible = Math.Max(10, (int)(multi.InstanceCount * GraphicsQuality.QualityRatio(_quality) * activity));
        multi.VisibleInstanceCount = Math.Min(visible, multi.InstanceCount);

        for (var i = 0; i < multi.VisibleInstanceCount; i++)
        {
            var district = state.Districts[i % state.Districts.Count];
            var center = DistrictPosition(district);
            var h = StableHash($"person:{i}");
            var alongX = (h & 1) == 0;
            var edge = ((h >> 2) & 1) == 0 ? 4.85f : -4.85f;
            var phase = (float)((_anim * (0.012 + (i % 11) * 0.0009) + Hash01(h >> 8)) % 1.0);
            var travel = Mathf.Lerp(-4.6f, 4.6f, phase);

            var pos = alongX
                ? center + new Vector3(travel, 0.48f, edge)
                : center + new Vector3(edge, 0.48f, travel);

            var bob = MathF.Sin((float)_anim * 5f + i) * 0.025f;
            pos.Y += bob;

            multi.SetInstanceTransform(i, new Transform3D(ScaledBasis(new Vector3(0.16f, 0.78f, 0.16f)), pos));
        }
    }

    private void UpdateAtmosphere()
    {
        if (_engine is null || _sun is null || _environment is null) return;

        var state = _engine.State;
        var daylight = Daylight(state.CurrentHour);
        var storm = state.Weather.Contains("Chuva forte", StringComparison.OrdinalIgnoreCase);
        var rain = state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase);
        var fog = state.Weather.Contains("Neblina", StringComparison.OrdinalIgnoreCase);

        var skyNight = new Color(0.010f, 0.018f, 0.045f);
        var skyDay = storm
            ? new Color(0.13f, 0.16f, 0.18f)
            : rain
                ? new Color(0.12f, 0.20f, 0.24f)
                : new Color(0.18f, 0.39f, 0.53f);

        _environment.BackgroundColor = skyNight.Lerp(skyDay, daylight);
        _environment.AmbientLightColor = new Color(0.25f, 0.32f, 0.42f).Lerp(new Color(0.72f, 0.77f, 0.78f), daylight);
        _environment.AmbientLightEnergy = 0.38f + daylight * (storm ? 0.34f : 0.62f);

        _sun.LightEnergy = 0.18f + daylight * (storm ? 0.65f : 1.42f);
        _sun.LightColor = new Color(0.50f, 0.60f, 0.82f).Lerp(new Color(1.0f, 0.89f, 0.72f), daylight);
        _sun.RotationDegrees = new Vector3(-38f - state.CurrentHour * 2.1f, -28f + state.CurrentHour * 4.0f, 0);

        _environment.FogEnabled = fog || rain;
        _environment.FogDensity = fog ? 0.022f : rain ? 0.006f : 0.0f;

        if (GraphicsQuality.RenderingMethod == "forward_plus")
        {
            _environment.VolumetricFogEnabled = _quality >= VisualQuality.Ultra && (fog || storm);
            _environment.SsaoEnabled = _quality >= VisualQuality.High;
            _environment.SsilEnabled = _quality >= VisualQuality.Ultra;
            _environment.GlowEnabled = _quality >= VisualQuality.High;
        }

        _weatherOverlay?.QueueRedraw();
    }

    private void TrackAdaptiveQuality(double delta)
    {
        if (Engine.IsEditorHint() || _manualQuality is not null) return;

        _performanceWindow += delta;
        _upgradeWindow += delta;
        _frameTimeSum += delta;
        _frameSamples++;

        if (_performanceWindow < 6.0 || _frameSamples < 30) return;

        var avgMs = _frameTimeSum / _frameSamples * 1000.0;
        _performanceWindow = 0;
        _frameTimeSum = 0;
        _frameSamples = 0;

        if (avgMs > 23.5 && _quality > VisualQuality.Low)
        {
            _quality--;
            _upgradeWindow = 0;
            ApplyQualityFeatures();
        }
        else if (avgMs < 13.5 && _upgradeWindow >= 12.0 && _quality < _ceiling)
        {
            _quality++;
            _upgradeWindow = 0;
            ApplyQualityFeatures();
        }
    }

    private void ApplyQualityFeatures()
    {
        var ratio = GraphicsQuality.QualityRatio(_quality);
        foreach (var (node, fullCount) in _scalableGroups)
        {
            if (node.Multimesh is null) continue;
            node.Multimesh.VisibleInstanceCount = Math.Clamp((int)MathF.Round(fullCount * ratio), 1, fullCount);
        }

        if (_sun is not null)
            _sun.ShadowEnabled = _quality >= VisualQuality.Medium;

        _externalAssets?.ApplyQuality(_quality);

        if (_environment is not null && GraphicsQuality.RenderingMethod == "forward_plus")
        {
            _environment.SsaoEnabled = _quality >= VisualQuality.High;
            _environment.SsilEnabled = _quality >= VisualQuality.Ultra;
            _environment.GlowEnabled = _quality >= VisualQuality.High;
            if (_quality < VisualQuality.Ultra)
                _environment.VolumetricFogEnabled = false;
        }
    }

    private void ApplyCamera()
    {
        if (_camera is null) return;
        _camera.Size = _cameraSize;
        var position = _cameraTarget + new Vector3(33f, 39f, 33f);
        _camera.Position = position;
        _camera.LookAt(_cameraTarget, Vector3.Up);
    }

    private static Mesh CreateBoxMesh(Vector3 size, Color color, float roughness)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = roughness,
            Metallic = 0.02f
        };

        return new BoxMesh
        {
            Size = size,
            Material = material
        };
    }

    private static StandardMaterial3D CreateVertexColorMaterial(float roughness, float metallic)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            VertexColorUseAsAlbedo = true,
            Roughness = roughness,
            Metallic = metallic
        };
    }

    private static Basis ScaledBasis(Vector3 scale) =>
        new(
            new Vector3(scale.X, 0, 0),
            new Vector3(0, scale.Y, 0),
            new Vector3(0, 0, scale.Z));

    private static Vector3 DistrictPosition(DistrictState district)
    {
        var x = (district.GridX - 1) * 14f;
        var z = (district.GridY - 1) * 14f;
        return new Vector3(x, 0, z);
    }

    private static Color DistrictColor(DistrictState district)
    {
        var wealth = (float)Math.Clamp(district.WealthIndex, 0.7m, 1.6m);
        var social = (float)Math.Clamp(district.SocialIndex, 0.7m, 1.4m);
        return new Color(
            0.055f + wealth * 0.022f,
            0.115f + social * 0.045f,
            0.115f + wealth * 0.030f);
    }

    private static Color ResidentialColor(int hash, DistrictState district)
    {
        var wealth = (float)Math.Clamp(district.WealthIndex, 0.7m, 1.6m);
        var t = Hash01(hash);
        return new Color(
            0.20f + wealth * 0.07f + t * 0.05f,
            0.31f + wealth * 0.08f + t * 0.04f,
            0.38f + wealth * 0.09f + t * 0.07f);
    }

    private static Color CompanyColor(CompanyState company)
    {
        if (company.PlayerOwned)
            return new Color(0.95f, 0.67f, 0.22f);

        var baseColor = SectorColor(company.Sector);
        if (company.OperatingStatus == "Crise")
            return baseColor.Darkened(0.42f);
        if (company.OperatingStatus == "Atenção")
            return baseColor.Lerp(new Color(0.82f, 0.52f, 0.18f), 0.24f);

        return baseColor.Lightened((float)company.BrandAwareness * 0.12f);
    }

    private static Color SectorColor(string sector)
    {
        var palette = new[]
        {
            new Color(0.28f, 0.63f, 0.88f),
            new Color(0.32f, 0.76f, 0.58f),
            new Color(0.52f, 0.45f, 0.80f),
            new Color(0.82f, 0.48f, 0.62f),
            new Color(0.91f, 0.61f, 0.30f),
            new Color(0.36f, 0.66f, 0.68f),
            new Color(0.43f, 0.55f, 0.82f),
            new Color(0.62f, 0.71f, 0.43f)
        };
        return palette[StableHash(sector) % palette.Length];
    }

    private static Color VehicleColor(int i)
    {
        var palette = new[]
        {
            new Color(0.80f, 0.84f, 0.86f),
            new Color(0.20f, 0.55f, 0.82f),
            new Color(0.83f, 0.29f, 0.25f),
            new Color(0.92f, 0.67f, 0.22f),
            new Color(0.18f, 0.23f, 0.27f),
            new Color(0.30f, 0.72f, 0.54f)
        };
        return palette[i % palette.Length];
    }

    private static Color PersonColor(int i)
    {
        var palette = new[]
        {
            new Color(0.91f, 0.47f, 0.42f),
            new Color(0.38f, 0.66f, 0.90f),
            new Color(0.93f, 0.72f, 0.36f),
            new Color(0.45f, 0.78f, 0.59f),
            new Color(0.70f, 0.55f, 0.85f),
            new Color(0.84f, 0.85f, 0.87f)
        };
        return palette[i % palette.Length];
    }

    private static float Daylight(int hour)
    {
        if (hour < 5 || hour >= 22) return 0.08f;
        if (hour < 7) return Mathf.Lerp(0.08f, 0.78f, (hour - 5) / 2f);
        if (hour < 18) return 1f;
        if (hour < 21) return Mathf.Lerp(1f, 0.14f, (hour - 18) / 3f);
        return 0.10f;
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

public partial class CityWeatherOverlay : Control
{
    private SimulationEngine? _engine;
    private double _time;

    public void SetEngine(SimulationEngine engine)
    {
        _engine = engine;
        QueueRedraw();
    }

    public void SetAnimationTime(double time)
    {
        _time = time;
        if (_engine?.State.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase) == true)
            QueueRedraw();
    }

    public override void _Draw()
    {
        if (_engine is null) return;
        var state = _engine.State;

        if (state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase))
        {
            var heavy = state.Weather.Contains("forte", StringComparison.OrdinalIgnoreCase);
            var count = heavy ? 90 : 52;
            for (var i = 0; i < count; i++)
            {
                var h = StableHash($"rain3d:{i}");
                var x = Hash01(h) * Size.X;
                var baseY = Hash01(h >> 7) * Size.Y;
                var y = (baseY + (float)_time * (heavy ? 380f : 285f)) % Math.Max(1f, Size.Y);
                DrawLine(
                    new Vector2(x, y),
                    new Vector2(x - (heavy ? 5f : 3f), y + (heavy ? 16f : 10f)),
                    new Color(0.68f, 0.84f, 0.96f, heavy ? 0.30f : 0.20f),
                    1f);
            }
        }

        if (state.Weather.Contains("Neblina", StringComparison.OrdinalIgnoreCase))
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.78f, 0.83f, 0.84f, 0.07f), true);

        var daylight = Daylight(state.CurrentHour);
        if (daylight < 0.25f)
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.01f, 0.02f, 0.05f, (0.25f - daylight) * 0.16f), true);
    }

    private static float Daylight(int hour)
    {
        if (hour < 5 || hour >= 22) return 0.08f;
        if (hour < 7) return Mathf.Lerp(0.08f, 0.78f, (hour - 5) / 2f);
        if (hour < 18) return 1f;
        if (hour < 21) return Mathf.Lerp(1f, 0.14f, (hour - 18) / 3f);
        return 0.10f;
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
