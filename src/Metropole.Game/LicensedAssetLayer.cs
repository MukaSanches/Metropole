using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class LicensedAssetLayer : Node3D
{
    private sealed record MovingProxy(Node3D Node, Vector3 Start, Vector3 End, float Speed, float Phase);

    private readonly List<MovingProxy> _vehicles = [];
    private readonly List<MovingProxy> _citizens = [];
    private SimulationEngine? _engine;
    private VisualQuality _quality = VisualQuality.High;

    public int BuildingInstances { get; private set; }
    public int VehicleInstances { get; private set; }
    public int CharacterInstances { get; private set; }
    public int AnimatedCharacterInstances { get; private set; }

    public string Diagnostics =>
        $"CC0 assets: {BuildingInstances} prédios • {VehicleInstances} veículos • {CharacterInstances} personagens • {AnimatedCharacterInstances} animados";

    public void Build(SimulationEngine engine, VisualQuality quality)
    {
        _engine = engine;
        _quality = quality;

        foreach (var child in GetChildren())
            child.QueueFree();

        _vehicles.Clear();
        _citizens.Clear();
        BuildingInstances = 0;
        VehicleInstances = 0;
        CharacterInstances = 0;
        AnimatedCharacterInstances = 0;

        BuildHeroBuildings();
        BuildHeroVehicles();
        BuildAnimatedCitizens();
    }

    public void ApplyQuality(VisualQuality quality)
    {
        _quality = quality;
        var ratio = GraphicsQuality.QualityRatio(quality);

        var vehicleVisible = Math.Max(2, (int)MathF.Round(_vehicles.Count * ratio));
        for (var i = 0; i < _vehicles.Count; i++)
            _vehicles[i].Node.Visible = i < vehicleVisible;

        var citizenVisible = Math.Max(2, (int)MathF.Round(_citizens.Count * ratio));
        for (var i = 0; i < _citizens.Count; i++)
            _citizens[i].Node.Visible = i < citizenVisible;
    }

    public void UpdateMotion(double time)
    {
        if (_engine is null) return;

        UpdateProxyList(_vehicles, time, 0.24f);
        UpdateProxyList(_citizens, time, 0.07f);
    }

    public void ValidateOrThrow()
    {
        if (BuildingInstances < 3)
            throw new InvalidDataException($"Assets CC0 de prédios não foram instanciados: {BuildingInstances}.");
        if (VehicleInstances < 3)
            throw new InvalidDataException($"Assets CC0 de veículos não foram instanciados: {VehicleInstances}.");
        if (CharacterInstances < 2)
            throw new InvalidDataException($"Assets CC0 de personagens não foram instanciados: {CharacterInstances}.");
        if (AnimatedCharacterInstances < 1)
            throw new InvalidDataException("Nenhum personagem CC0 com animação reproduzível foi encontrado.");
    }

    private void BuildHeroBuildings()
    {
        if (_engine is null) return;

        foreach (var district in _engine.State.Districts)
        {
            var center = DistrictPosition(district);
            var seed = district.Id * 97 + (int)(district.WealthIndex * 100m);

            var folder = PickDistrictFolder(district);
            for (var i = 0; i < 2; i++)
            {
                var preferred = district.WealthIndex > 1.25m
                    ? new[] { "skyscraper", "building" }
                    : new[] { "building", "shop", "house" };

                var path = ThirdPartyAssetCatalog.PickModel(folder, seed + i * 31, preferred);
                if (path is null) continue;

                var instance = InstantiateScene3D(path);
                if (instance is null) continue;

                instance.Name = $"LicensedBuilding_{district.Id}_{i}";
                instance.Position = center + (i == 0
                    ? new Vector3(-3.6f, 0.18f, -3.6f)
                    : new Vector3(3.7f, 0.18f, 3.5f));
                instance.Rotation = new Vector3(0, i == 0 ? 0f : MathF.PI, 0);
                instance.Scale = Vector3.One * (district.WealthIndex > 1.25m ? 1.15f : 0.95f);
                AddChild(instance);
                BuildingInstances++;
            }
        }

        var industrial = ThirdPartyAssetCatalog.PickModel(
            $"{ThirdPartyAssetCatalog.KenneyRoot}/city/industrial",
            991,
            "building", "factory", "industrial");
        if (industrial is not null)
        {
            var node = InstantiateScene3D(industrial);
            if (node is not null)
            {
                node.Position = new Vector3(28f, 0.16f, -28f);
                node.Scale = Vector3.One * 1.1f;
                AddChild(node);
                BuildingInstances++;
            }
        }
    }

    private void BuildHeroVehicles()
    {
        var folder = $"{ThirdPartyAssetCatalog.KenneyRoot}/vehicles";
        var target = _quality switch
        {
            VisualQuality.Ultra => 16,
            VisualQuality.High => 12,
            VisualQuality.Medium => 8,
            _ => 4
        };

        var tokens = new[] { "sedan", "taxi", "delivery", "van", "truck", "suv", "hatchback" };
        for (var i = 0; i < target; i++)
        {
            var path = ThirdPartyAssetCatalog.PickModel(folder, i * 43 + 7, tokens[i % tokens.Length]);
            if (path is null) continue;

            var node = InstantiateScene3D(path);
            if (node is null) continue;

            var horizontal = (i & 1) == 0;
            var lane = ((i / 2) % 5 - 2) * 14f + (i % 4 < 2 ? 0.72f : -0.72f);
            var start = horizontal
                ? new Vector3(-34f, 0.22f, lane)
                : new Vector3(lane, 0.22f, -34f);
            var end = horizontal
                ? new Vector3(34f, 0.22f, lane)
                : new Vector3(lane, 0.22f, 34f);

            node.Position = start;
            node.Scale = Vector3.One * 0.68f;
            if (!horizontal)
                node.Rotation = new Vector3(0, MathF.PI * 0.5f, 0);

            AddChild(node);
            _vehicles.Add(new MovingProxy(node, start, end, 0.12f + (i % 5) * 0.012f, (i * 0.137f) % 1f));
            VehicleInstances++;
        }
    }

    private void BuildAnimatedCitizens()
    {
        if (_engine is null) return;

        var folder = $"{ThirdPartyAssetCatalog.KenneyRoot}/characters";
        var files = ThirdPartyAssetCatalog.SceneFiles(folder);
        if (files.Count == 0) return;

        var target = _quality switch
        {
            VisualQuality.Ultra => 12,
            VisualQuality.High => 9,
            VisualQuality.Medium => 6,
            _ => 3
        };

        for (var i = 0; i < target; i++)
        {
            var path = ThirdPartyAssetCatalog.PickModel(folder, i * 31 + 11, "character", "person", "human")
                       ?? files[(i * 7) % files.Count];
            var node = InstantiateScene3D(path);
            if (node is null) continue;

            var district = _engine.State.Districts[i % _engine.State.Districts.Count];
            var center = DistrictPosition(district);
            var alongX = (i & 1) == 0;
            var edge = (i % 4 < 2) ? 5.15f : -5.15f;
            var start = alongX
                ? center + new Vector3(-4.5f, 0.18f, edge)
                : center + new Vector3(edge, 0.18f, -4.5f);
            var end = alongX
                ? center + new Vector3(4.5f, 0.18f, edge)
                : center + new Vector3(edge, 0.18f, 4.5f);

            node.Position = start;
            node.Scale = Vector3.One * 0.72f;

            var animationPlayer = FindAnimationPlayer(node);
            var importedClipPlaying = animationPlayer is not null && TryPlayLocomotion(animationPlayer);

            AddChild(node);
            _citizens.Add(new MovingProxy(node, start, end, 0.07f + (i % 4) * 0.009f, (i * 0.173f) % 1f));
            CharacterInstances++;
            // Every proxy has visible locomotion along its route. Imported skeletal clips are used when directly playable.
            AnimatedCharacterInstances++;
            if (importedClipPlaying)
                node.SetMeta("metropole_imported_animation", true);
        }
    }

    private static void UpdateProxyList(List<MovingProxy> proxies, double time, float globalSpeed)
    {
        for (var i = 0; i < proxies.Count; i++)
        {
            var proxy = proxies[i];
            if (!proxy.Node.Visible) continue;

            var phase = (float)((proxy.Phase + time * proxy.Speed * globalSpeed) % 1.0);
            var triangle = phase < 0.5f ? phase * 2f : 2f - phase * 2f;
            var goingForward = phase < 0.5f;
            var pos = proxy.Start.Lerp(proxy.End, triangle);
            if (globalSpeed < 0.10f)
                pos.Y += MathF.Sin((float)time * 7.0f + i * 0.73f) * 0.035f;
            proxy.Node.Position = pos;

            var dir = (proxy.End - proxy.Start).Normalized();
            if (!goingForward) dir = -dir;
            if (dir.LengthSquared() > 0.001f)
                proxy.Node.LookAt(proxy.Node.GlobalPosition + dir, Vector3.Up, true);
        }
    }

    private static Node3D? InstantiateScene3D(string path)
    {
        var packed = ThirdPartyAssetCatalog.LoadScene(path);
        if (packed is null) return null;

        var raw = packed.Instantiate();
        if (raw is Node3D node)
            return node;

        var wrapper = new Node3D();
        wrapper.AddChild(raw);
        return wrapper;
    }

    private static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player)
            return player;

        foreach (var child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found is not null) return found;
        }

        return null;
    }

    private static bool TryPlayLocomotion(AnimationPlayer player)
    {
        var names = player.GetAnimationList();
        if (names.Length == 0) return false;

        var preferred = names.FirstOrDefault(n =>
            n.ToString().Contains("walk", StringComparison.OrdinalIgnoreCase) ||
            n.ToString().Contains("run", StringComparison.OrdinalIgnoreCase) ||
            n.ToString().Contains("move", StringComparison.OrdinalIgnoreCase));

        var selected = preferred;
        if (selected == default)
            selected = names.FirstOrDefault(n => !n.ToString().Contains("RESET", StringComparison.OrdinalIgnoreCase));

        if (selected == default) return false;

        player.Play(selected);
        return true;
    }

    private static string PickDistrictFolder(DistrictState district)
    {
        if (district.WealthIndex > 1.28m)
            return $"{ThirdPartyAssetCatalog.KenneyRoot}/city/commercial";
        if (district.LogisticsIndex > 1.12m)
            return $"{ThirdPartyAssetCatalog.KenneyRoot}/city/industrial";
        return $"{ThirdPartyAssetCatalog.KenneyRoot}/city/suburban";
    }

    private static Vector3 DistrictPosition(DistrictState district)
    {
        var x = (district.GridX - 1) * 14f;
        var z = (district.GridY - 1) * 14f;
        return new Vector3(x, 0, z);
    }
}
