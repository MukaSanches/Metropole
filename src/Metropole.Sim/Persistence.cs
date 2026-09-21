using System.Text.Json;

namespace Metropole.Sim;

public static class SaveStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = false
    };

    public static void Save(string path, GameState state)
    {
        SimulationValidator.Validate(state);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";
        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(tempPath, json);

        var validation = JsonSerializer.Deserialize<GameState>(File.ReadAllText(tempPath), Options)
            ?? throw new InvalidDataException("Falha ao validar save temporário.");
        ValidateSchema(validation);
        SimulationValidator.Validate(validation);

        if (File.Exists(path)) File.Copy(path, backupPath, true);
        File.Move(tempPath, path, true);
    }

    public static GameState Load(string path)
    {
        try
        {
            return LoadOne(path);
        }
        catch when (File.Exists(path + ".bak"))
        {
            return LoadOne(path + ".bak");
        }
    }

    private static GameState LoadOne(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Save não encontrado.", path);
        var state = JsonSerializer.Deserialize<GameState>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Save vazio ou inválido.");
        ValidateSchema(state);
        SimulationValidator.Validate(state);
        return state;
    }

    private static void ValidateSchema(GameState state)
    {
        if (state.SchemaVersion > GameState.CurrentSchemaVersion)
            throw new InvalidDataException($"Save usa schema futuro {state.SchemaVersion}.");
        if (state.SchemaVersion < 1)
            throw new InvalidDataException($"Schema legado não suportado: {state.SchemaVersion}.");
        // Schema 1 é o schema inicial. Migrações futuras entram aqui antes da validação.
    }
}
