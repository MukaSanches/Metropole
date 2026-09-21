using Godot;

namespace Metropole.Game;

public static class ExternalAssetLibrary
{
    public static readonly string[] BuildingScenes =
    [
        "res://assets/external/kenney_city/building-a.glb",
        "res://assets/external/kenney_city/building-e.glb",
        "res://assets/external/kenney_city/building-j.glb",
        "res://assets/external/kenney_city/building-skyscraper-a.glb",
        "res://assets/external/kenney_city/building-skyscraper-d.glb",
        "res://assets/external/kenney_city/building-s.glb"
    ];

    public static readonly string[] CharacterScenes =
    [
        "res://assets/external/quaternius_people/civilian_man.glb",
        "res://assets/external/quaternius_people/civilian_suit.glb",
        "res://assets/external/quaternius_people/civilian_casual.glb",
        "res://assets/external/quaternius_people/civilian_longsleeve.glb",
        "res://assets/external/quaternius_people/civilian_woman.glb",
        "res://assets/external/quaternius_people/civilian_woman2.glb"
    ];

    public static readonly string[] PropScenes =
    [
        "res://assets/external/kenney_city/light-curved.glb",
        "res://assets/external/kenney_city/construction-cone.glb"
    ];

    public static readonly string[] RoadScenes =
    [
        "res://assets/external/kenney_city/road-straight.glb",
        "res://assets/external/kenney_city/road-crossroad.glb"
    ];

    public static readonly string[] AudioAssets =
    [
        "res://assets/external/audio/city_traffic.ogg",
        "res://assets/external/audio/rain_window_loop.wav",
        "res://assets/external/audio/wind_loop.ogg",
        "res://assets/external/audio/ui_click.ogg",
        "res://assets/external/audio/ui_hover.ogg"
    ];

    private static readonly Dictionary<string, PackedScene> SceneCache = new(StringComparer.Ordinal);

    public static PackedScene? Scene(string path)
    {
        if (SceneCache.TryGetValue(path, out var cached))
            return cached;

        if (!ResourceLoader.Exists(path))
            return null;

        var scene = GD.Load<PackedScene>(path);
        if (scene is not null)
            SceneCache[path] = scene;

        return scene;
    }

    public static Node3D? InstantiateBuilding(int variant, Node parent, Vector3 position, float scale = 1f)
    {
        var path = BuildingScenes[Math.Abs(variant) % BuildingScenes.Length];
        return InstantiateScene(path, parent, position, scale, $"AssetBuilding_{variant}");
    }

    public static Node3D? InstantiateProp(int variant, Node parent, Vector3 position, float scale = 1f)
    {
        var path = PropScenes[Math.Abs(variant) % PropScenes.Length];
        return InstantiateScene(path, parent, position, scale, $"AssetProp_{variant}");
    }

    public static Node3D? InstantiateCharacter(int variant, Node parent, Vector3 position, float scale = 1f)
    {
        var path = CharacterScenes[Math.Abs(variant) % CharacterScenes.Length];
        var node = InstantiateScene(path, parent, position, scale, $"AnimatedCitizen_{variant}");
        if (node is null) return null;

        var player = FindAnimationPlayer(node);
        if (player is not null)
        {
            player.SpeedScale = 0.88f + (variant % 5) * 0.035f;
            PlayPreferredAnimation(player, "Walk");
        }

        return node;
    }

    public static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer animationPlayer)
            return animationPlayer;

        foreach (Node child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found is not null)
                return found;
        }

        return null;
    }

    public static bool PlayPreferredAnimation(AnimationPlayer player, string token)
    {
        var animations = player.GetAnimationList();
        var selected = animations
            .Select(x => x.ToString())
            .FirstOrDefault(x => x.Contains(token, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(selected))
        {
            selected = animations
                .Select(x => x.ToString())
                .FirstOrDefault(x => !x.Equals("RESET", StringComparison.OrdinalIgnoreCase));
        }

        if (string.IsNullOrWhiteSpace(selected))
            return false;

        player.Play(selected);
        return true;
    }

    public static string ValidateImportedAssets()
    {
        var missing = new List<string>();
        var loadErrors = new List<string>();
        var animatedCharacters = 0;
        var animationClips = 0;

        foreach (var path in BuildingScenes.Concat(PropScenes).Concat(RoadScenes))
        {
            if (!ResourceLoader.Exists(path))
            {
                missing.Add(path);
                continue;
            }

            if (Scene(path) is null)
                loadErrors.Add(path);
        }

        foreach (var path in CharacterScenes)
        {
            if (!ResourceLoader.Exists(path))
            {
                missing.Add(path);
                continue;
            }

            var scene = Scene(path);
            if (scene is null)
            {
                loadErrors.Add(path);
                continue;
            }

            var instance = scene.Instantiate();
            try
            {
                var player = FindAnimationPlayer(instance);
                if (player is null)
                {
                    loadErrors.Add($"{path} (sem AnimationPlayer)");
                    continue;
                }

                var clips = player.GetAnimationList()
                    .Select(x => x.ToString())
                    .Where(x => !x.Equals("RESET", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (clips.Length == 0)
                {
                    loadErrors.Add($"{path} (sem clips)");
                    continue;
                }

                animatedCharacters++;
                animationClips += clips.Length;
            }
            finally
            {
                instance.Free();
            }
        }

        foreach (var path in AudioAssets)
        {
            if (!ResourceLoader.Exists(path))
            {
                missing.Add(path);
                continue;
            }

            if (GD.Load<AudioStream>(path) is null)
                loadErrors.Add(path);
        }

        if (missing.Count > 0 || loadErrors.Count > 0)
            throw new InvalidOperationException(
                $"Assets externos inválidos. Ausentes: {string.Join(", ", missing)}. Falhas: {string.Join(", ", loadErrors)}.");

        if (animatedCharacters < 4)
            throw new InvalidOperationException($"Poucos personagens animados válidos: {animatedCharacters}.");

        return $"buildings={BuildingScenes.Length} characters={animatedCharacters} clips={animationClips} audio={AudioAssets.Length}";
    }

    private static Node3D? InstantiateScene(string path, Node parent, Vector3 position, float scale, string name)
    {
        var scene = Scene(path);
        if (scene is null) return null;

        var raw = scene.Instantiate();
        if (raw is not Node3D node)
        {
            raw.Free();
            return null;
        }

        node.Name = name;
        node.Position = position;
        node.Scale = Vector3.One * scale;
        parent.AddChild(node);
        return node;
    }
}
