using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class GameAudio : Node
{
    private AudioStreamPlayer? _ui;
    private AudioStreamPlayer? _city;
    private AudioStreamPlayer? _rain;

    private AudioStream? _click;
    private AudioStream? _hover;
    private AudioStream? _confirm;
    private AudioStream? _error;
    private AudioStream? _open;
    private AudioStream? _back;

    private bool _rainWanted;
    public int LoadedAssetCount { get; private set; }

    public override void _Ready()
    {
        Name = "GameAudio";

        _click = Load("res://assets/external/kenney/audio/ui-click.wav");
        _hover = Load("res://assets/external/kenney/audio/ui-select.wav");
        _confirm = Load("res://assets/external/kenney/audio/ui-confirm.wav");
        _error = Load("res://assets/external/kenney/audio/ui-error.wav");
        _open = Load("res://assets/external/kenney/audio/ui-open.wav");
        _back = Load("res://assets/external/kenney/audio/ui-back.wav");

        _ui = new AudioStreamPlayer { VolumeDb = -10f };
        AddChild(_ui);

        _city = CreateLoopingPlayer(
            Load("res://assets/external/opengameart/audio/city-outdoor.ogg"),
            -28f);
        _rain = CreateLoopingPlayer(
            Load("res://assets/external/opengameart/audio/rain-loop.ogg"),
            -80f);

        AddChild(_city);
        AddChild(_rain);

        if (_city.Stream is not null)
            _city.Play();
    }

    public void PlayClick() => PlayUi(_click, -11f);
    public void PlayHover() => PlayUi(_hover, -22f);
    public void PlayConfirm() => PlayUi(_confirm, -9f);
    public void PlayError() => PlayUi(_error, -8f);
    public void PlayOpen() => PlayUi(_open, -12f);
    public void PlayBack() => PlayUi(_back, -12f);

    public void UpdateAmbience(GameState state)
    {
        if (_city is null || _rain is null) return;

        var night = state.CurrentHour is >= 22 or < 6;
        _city.VolumeDb = night ? -34f : -27f;

        _rainWanted = state.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase);
        if (_rainWanted)
        {
            if (!_rain.Playing && _rain.Stream is not null)
                _rain.Play();

            var heavy = state.Weather.Contains("forte", StringComparison.OrdinalIgnoreCase);
            _rain.VolumeDb = heavy ? -16f : -22f;
        }
        else
        {
            _rain.VolumeDb = -80f;
            if (_rain.Playing)
                _rain.Stop();
        }
    }

    private AudioStream? Load(string path)
    {
        var stream = GD.Load<AudioStream>(path);
        if (stream is not null)
            LoadedAssetCount++;
        else
            GD.PushWarning($"Audio asset not available: {path}");
        return stream;
    }

    private AudioStreamPlayer CreateLoopingPlayer(AudioStream? stream, float volumeDb)
    {
        var player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = volumeDb
        };
        player.Finished += () =>
        {
            if (player.Stream is not null && (player == _city || _rainWanted))
                player.Play();
        };
        return player;
    }

    private void PlayUi(AudioStream? stream, float volumeDb)
    {
        if (_ui is null || stream is null) return;
        _ui.Stop();
        _ui.Stream = stream;
        _ui.VolumeDb = volumeDb;
        _ui.Play();
    }
}
