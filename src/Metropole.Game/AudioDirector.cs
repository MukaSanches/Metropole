using Godot;
using Metropole.Sim;

namespace Metropole.Game;

public partial class AudioDirector : Node
{
    private readonly List<AudioStream> _uiStreams = [];
    private AudioStreamPlayer? _uiPlayer;
    private AudioStreamPlayer? _rainPlayer;
    private AudioStreamPlayer? _crowdPlayer;
    private SimulationEngine? _engine;
    private int _clickIndex;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        EnsureBus("UI");
        EnsureBus("Ambience");
        EnsureBus("Weather");

        _uiPlayer = new AudioStreamPlayer { Bus = "UI", VolumeDb = -8f };
        _rainPlayer = new AudioStreamPlayer { Bus = "Weather", VolumeDb = -80f };
        _crowdPlayer = new AudioStreamPlayer { Bus = "Ambience", VolumeDb = -80f };
        AddChild(_uiPlayer);
        AddChild(_rainPlayer);
        AddChild(_crowdPlayer);

        foreach (var path in ThirdPartyAssetCatalog.AudioFiles($"{ThirdPartyAssetCatalog.AudioRoot}/ui").Take(16))
        {
            var stream = ResourceLoader.Load<AudioStream>(path);
            if (stream is not null)
                _uiStreams.Add(stream);
        }

        var rain = ResourceLoader.Load<AudioStream>($"{ThirdPartyAssetCatalog.AudioRoot}/weather/rain.ogg");
        if (rain is AudioStreamOggVorbis rainOgg)
            rainOgg.Loop = true;
        if (rain is not null)
        {
            _rainPlayer.Stream = rain;
            _rainPlayer.Play();
        }

        var crowd = ResourceLoader.Load<AudioStream>($"{ThirdPartyAssetCatalog.AudioRoot}/city-crowd.ogg");
        if (crowd is AudioStreamOggVorbis crowdOgg)
            crowdOgg.Loop = true;
        if (crowd is not null)
        {
            _crowdPlayer.Stream = crowd;
            _crowdPlayer.Play();
        }
    }

    public void SetSimulation(SimulationEngine? engine) => _engine = engine;

    public void PlayClick()
    {
        if (_uiPlayer is null || _uiStreams.Count == 0) return;
        _uiPlayer.Stream = _uiStreams[_clickIndex % _uiStreams.Count];
        _clickIndex = (_clickIndex + 3) % _uiStreams.Count;
        _uiPlayer.PitchScale = 0.96f + (_clickIndex % 5) * 0.02f;
        _uiPlayer.Play();
    }

    public void PlayHover()
    {
        if (_uiPlayer is null || _uiStreams.Count < 2 || _uiPlayer.Playing) return;
        _uiPlayer.Stream = _uiStreams[(_clickIndex + 1) % _uiStreams.Count];
        _uiPlayer.PitchScale = 1.08f;
        _uiPlayer.VolumeDb = -15f;
        _uiPlayer.Play();
        _uiPlayer.VolumeDb = -8f;
    }

    public override void _Process(double delta)
    {
        if (_rainPlayer is null || _crowdPlayer is null) return;

        var rainTarget = -80f;
        var crowdTarget = -80f;

        if (_engine is not null)
        {
            var s = _engine.State;
            if (s.Weather.Contains("Chuva forte", StringComparison.OrdinalIgnoreCase))
                rainTarget = -8f;
            else if (s.Weather.Contains("Chuva", StringComparison.OrdinalIgnoreCase))
                rainTarget = -14f;

            var busyHour = s.CurrentHour is >= 7 and <= 21;
            if (busyHour)
            {
                var populationFactor = Math.Clamp(s.Population / 1800f, 0.25f, 1f);
                crowdTarget = Mathf.Lerp(-34f, -22f, populationFactor);
            }
        }

        var step = (float)(delta * 10.0);
        _rainPlayer.VolumeDb = Mathf.MoveToward(_rainPlayer.VolumeDb, rainTarget, step);
        _crowdPlayer.VolumeDb = Mathf.MoveToward(_crowdPlayer.VolumeDb, crowdTarget, step * 0.7f);
    }

    private static void EnsureBus(string name)
    {
        if (AudioServer.GetBusIndex(name) >= 0) return;
        AudioServer.AddBus();
        var idx = AudioServer.BusCount - 1;
        AudioServer.SetBusName(idx, name);
    }
}
