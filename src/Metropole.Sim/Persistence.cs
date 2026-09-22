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
        MigrateSchema(state);
        SimulationValidator.Validate(state);
        AaaSimulationValidator.Validate(state);
        EverydayLife.Validate(state);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        var backupPath = path + ".bak";
        var json = JsonSerializer.Serialize(state, Options);
        File.WriteAllText(tempPath, json);

        var validation = JsonSerializer.Deserialize<GameState>(File.ReadAllText(tempPath), Options)
            ?? throw new InvalidDataException("Falha ao validar save temporário.");
        MigrateSchema(validation);
        ValidateSchema(validation);
        SimulationValidator.Validate(validation);
        AaaSimulationValidator.Validate(validation);
        EverydayLife.Validate(validation);

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
        MigrateSchema(state);
        ValidateSchema(state);
        SimulationValidator.Validate(state);
        AaaSimulationValidator.Validate(state);
        EverydayLife.Validate(state);
        return state;
    }

    private static void MigrateSchema(GameState state)
    {
        if (state.SchemaVersion == 1)
        {
            state.SchemaVersion = 2;
            state.RulesVersion = "1.6.0";
            if (state.Aaa is null) state.Aaa = new AaaWorldState();
        }
        if (state.SchemaVersion == 2)
        {
            state.SchemaVersion = 3;
            state.RulesVersion = "1.9.0";
            EverydayLife.Initialize(state);
        }
    }

    private static void ValidateSchema(GameState state)
    {
        if (state.SchemaVersion > GameState.CurrentSchemaVersion)
            throw new InvalidDataException($"Save usa schema futuro {state.SchemaVersion}.");
        if (state.SchemaVersion < 1)
            throw new InvalidDataException($"Schema legado não suportado: {state.SchemaVersion}.");
        // Schema 1 é migrado para schema 2 antes desta validação.
    }
}
