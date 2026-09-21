using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class GameAudioDirector : Node
{
    private const string BusUi = "UI";
    private const string BusAmbience = "Ambience";
    private const string BusWeather = "Weather";

    private SimulationEngine? _engine;
    private AudioStreamPlayer? _traffic;
    private AudioStreamPlayer? _rain;
    private AudioStreamPlayer? _wind;
    private AudioStreamPlayer? _uiClick;
    private AudioStreamPlayer? _uiHover;
    private ulong _lastHoverMs;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        EnsureBus(BusUi, -4f);
        EnsureBus(BusAmbience, -8f);
        EnsureBus(BusWeather, -7f);

        _traffic = MakeLoopPlayer("res://assets/external/audio/city_traffic.ogg", BusAmbience, -42f);
        _rain = MakeLoopPlayer("res://assets/external/audio/rain_window_loop.wav", BusWeather, -60f);
        _wind = MakeLoopPlayer("res://assets/external/audio/wind_loop.ogg", BusWeather, -48f);

        _uiClick = MakeOneShotPlayer("res://assets/external/audio/ui_click.ogg", BusUi, -9f, 5);
        _uiHover = MakeOneShotPlayer("res://assets/external/audio/ui_hover.ogg", BusUi, -15f, 3);

        SetProcess(true);
    }

    public void SetSimulation(SimulationEngine? engine)
    {
        _engine = engine;
        if (_traffic is not null && !_traffic.Playing) _traffic.Play();
        if (_rain is not null && !_rain.Playing) _rain.Play();
        if (_wind is not null && !_wind.Playing) _wind.Play();
    }

    public void PlayClick()
    {
        if (_uiClick is null) return;
        _uiClick.Play();
    }

    public void PlayHover()
    {
        if (_uiHover is null) return;
        var now = Time.GetTicksMsec();
        if (now - _lastHoverMs < 70) return;
        _lastHoverMs = now;
        _uiHover.Play();
    }

    public override void _Process(double delta)
    {
        var dt = (float)Math.Clamp(delta * 2.2, 0.01, 0.25);

        if (_engine is null)
        {
            Fade(_traffic, -36f, dt);
            Fade(_rain, -60f, dt);
            Fade(_wind, -42f, dt);
            return;
        }

        var state = _engine.State;
        var rush = state.CurrentHour is >= 7 and <= 9 or >= 16 and <= 19;
        var night = state.CurrentHour is >= 0 and <= 5;
        var raining = state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase);
        var heavyRain = state.Weather.Contains("forte", StringComparison.OrdinalIgnoreCase);
        var fog = state.Weather.Contains("Neblina", StringComparison.OrdinalIgnoreCase);
        var cloudy = state.Weather.Contains("Nublado", StringComparison.OrdinalIgnoreCase);

        var trafficTarget = night ? -27f : rush ? -15f : -20f;
        var rainTarget = raining ? (heavyRain ? -7f : -12f) : -60f;
        var windTarget = heavyRain ? -18f : fog ? -21f : cloudy ? -27f : -38f;

        Fade(_traffic, trafficTarget, dt);
        Fade(_rain, rainTarget, dt);
        Fade(_wind, windTarget, dt);
    }

    private AudioStreamPlayer MakeLoopPlayer(string path, string bus, float volume)
    {
        var stream = GD.Load<AudioStream>(path)
            ?? throw new InvalidOperationException($"Áudio obrigatório não carregou: {path}");
        EnableLoop(stream);

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = bus,
            VolumeDb = volume,
            Autoplay = true,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(player);
        return player;
    }

    private AudioStreamPlayer MakeOneShotPlayer(string path, string bus, float volume, int polyphony)
    {
        var stream = GD.Load<AudioStream>(path)
            ?? throw new InvalidOperationException($"Áudio obrigatório não carregou: {path}");

        var player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = bus,
            VolumeDb = volume,
            MaxPolyphony = polyphony,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(player);
        return player;
    }

    private static void EnableLoop(AudioStream stream)
    {
        switch (stream)
        {
            case AudioStreamOggVorbis ogg:
                ogg.Loop = true;
                break;
            case AudioStreamWav wav:
                wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
                break;
        }
    }

    private static void Fade(AudioStreamPlayer? player, float targetDb, float weight)
    {
        if (player is null) return;
        if (!player.Playing) player.Play();
        player.VolumeDb = Mathf.Lerp(player.VolumeDb, targetDb, weight);
    }

    private static void EnsureBus(string name, float volumeDb)
    {
        var index = AudioServer.GetBusIndex(name);
        if (index < 0)
        {
            AudioServer.AddBus();
            index = AudioServer.BusCount - 1;
            AudioServer.SetBusName(index, name);
        }

        AudioServer.SetBusVolumeDb(index, volumeDb);
    }
}
