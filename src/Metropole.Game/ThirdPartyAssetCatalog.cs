using Godot;

namespace Metropole.Game;

public readonly record struct ThirdPartyAssetStats(
    int CityModels,
    int VehicleModels,
    int CharacterModels,
    int UiSounds,
    bool HasRain,
    bool HasCrowd);

public static class ThirdPartyAssetCatalog
{
    public const string Root = "res://assets/third_party";
    public const string KenneyRoot = Root + "/kenney";
    public const string AudioRoot = Root + "/audio";

    public static IReadOnlyList<string> SceneFiles(string folder)
    {
        if (!DirAccess.DirExistsAbsolute(folder))
            return Array.Empty<string>();

        return DirAccess.GetFilesAt(folder)
            .Where(IsSceneAsset)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{folder}/{x}")
            .ToArray();
    }

    public static IReadOnlyList<string> AudioFiles(string folder)
    {
        if (!DirAccess.DirExistsAbsolute(folder))
            return Array.Empty<string>();

        return DirAccess.GetFilesAt(folder)
            .Where(x =>
                x.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{folder}/{x}")
            .ToArray();
    }

    public static PackedScene? LoadScene(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
            return null;
        return ResourceLoader.Load<PackedScene>(path);
    }

    public static string? PickModel(string folder, int seed, params string[] preferredTokens)
    {
        var files = SceneFiles(folder);
        if (files.Count == 0) return null;

        foreach (var token in preferredTokens)
        {
            var matches = files.Where(x => x.Contains(token, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length > 0)
                return matches[Math.Abs(seed) % matches.Length];
        }

        return files[Math.Abs(seed) % files.Count];
    }

    public static ThirdPartyAssetStats Stats()
    {
        var city =
            SceneFiles($"{KenneyRoot}/city/commercial").Count +
            SceneFiles($"{KenneyRoot}/city/industrial").Count +
            SceneFiles($"{KenneyRoot}/city/suburban").Count +
            SceneFiles($"{KenneyRoot}/city/roads").Count;

        var vehicles = SceneFiles($"{KenneyRoot}/vehicles").Count;
        var characters = SceneFiles($"{KenneyRoot}/characters").Count;
        var ui = AudioFiles($"{AudioRoot}/ui").Count;

        return new ThirdPartyAssetStats(
            city,
            vehicles,
            characters,
            ui,
            ResourceLoader.Exists($"{AudioRoot}/weather/rain.ogg"),
            ResourceLoader.Exists($"{AudioRoot}/city-crowd.ogg"));
    }

    public static void ValidateOrThrow()
    {
        var s = Stats();
        if (s.CityModels < 40)
            throw new InvalidDataException($"Poucos assets de cidade importados: {s.CityModels}.");
        if (s.VehicleModels < 8)
            throw new InvalidDataException($"Poucos veículos importados: {s.VehicleModels}.");
        if (s.CharacterModels < 4)
            throw new InvalidDataException($"Poucos personagens importados: {s.CharacterModels}.");
        if (s.UiSounds < 20)
            throw new InvalidDataException($"Poucos sons de UI importados: {s.UiSounds}.");
        if (!s.HasRain)
            throw new InvalidDataException("Áudio de chuva não foi importado.");
        if (!s.HasCrowd)
            throw new InvalidDataException("Áudio de cidade não foi importado.");
    }

    private static bool IsSceneAsset(string file) =>
        file.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
        file.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) ||
        file.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
}
