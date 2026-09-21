# Asset provenance — METRÓPOLE ∞ 1.4

Audit date: 2026-09-21

This document records every third-party runtime asset introduced in 1.4. The game does not fetch these assets at runtime. Copies are vendored inside `src/Metropole.Game/assets/external` so builds are reproducible and offline.

## Licensing rule

Only assets with a documented redistribution-friendly license were accepted. The selected runtime assets are CC0 1.0 / public-domain-dedicated. Attribution is therefore not legally required by CC0, but provenance is retained for auditability and respect for the creators.

## Kenney — city kit assets

Creator: Kenney  
License: CC0 1.0 for the asset files.  
Official source family:
- https://kenney.nl/assets/city-kit-commercial
- https://kenney.nl/assets/city-kit-suburban
- https://kenney.nl/assets/city-kit-industrial
- https://kenney.nl/assets/city-kit-roads

Byte mirror used for deterministic repository import:
- https://github.com/Rbitah/godot-city-builder_kenny_assets
- mirror README explicitly states that included 2D/3D/sound assets are CC0.

Vendored files:
- `assets/external/kenney_city/building-a.glb`
- `building-e.glb`
- `building-j.glb`
- `building-skyscraper-a.glb`
- `building-skyscraper-d.glb`
- `building-s.glb`
- `road-straight.glb`
- `road-crossroad.glb`
- `light-curved.glb`
- `construction-cone.glb`

Use in METRÓPOLE:
- high-detail district landmark buildings;
- premium-profile street props;
- reserved road modules for future street replacement;
- procedural/MultiMesh geometry remains the scalable fallback.

## Quaternius — Animated Men + Animated Women

Creator: Quaternius  
License: CC0 1.0 Universal / Public Domain Dedication.  
Official creator site:
- https://quaternius.com/packs/animatedmen.html
- https://quaternius.com/packs/animatedwomen.html

Audited byte source:
- https://github.com/MrArun005/3D-Games-AmusementPark
- source commit: `2d827a479ef7a44938372ca07d24c0faffb43b1d`
- bundled `LICENSE-Quaternius.txt` identifies both packs as CC0.

Vendored files:
- `assets/external/quaternius_people/civilian_man.glb`
- `civilian_suit.glb`
- `civilian_casual.glb`
- `civilian_longsleeve.glb`
- `civilian_woman.glb`
- `civilian_woman2.glb`

The source audit states these GLBs are rigged and contain embedded clips including Idle, Walk, Run, Jump, Sitting, Standing, Clapping, Punch and Death. METRÓPOLE validates imported AnimationPlayer data during release CI and currently uses walking clips for near-camera citizen proxies.

## Kenney — UI Audio

Creator: Kenney  
License: CC0 1.0.  
Official source:
- https://kenney.nl/assets/ui-audio

Byte source:
- https://github.com/iree-gd/iree.gd
- runtime copies are original Kenney UI audio samples.

Vendored files:
- `assets/external/audio/ui_click.ogg`
- `assets/external/audio/ui_hover.ogg`

Use:
- button press and hover feedback through the UI audio bus.

## OpenGameArt — city ambience

### High traffic road sounds
Creator: IgnasD  
Source: https://opengameart.org/content/high-traffic-road-sounds  
Original file: `gatve Varniu.ogg`  
License: CC0 1.0.

Vendored as:
- `assets/external/audio/city_traffic.ogg`

### Rain on Window Loop
Creator: alxl  
Source: https://opengameart.org/content/rain-on-window-loop  
Original file: `rain_on_window_loop.wav`  
License option selected by the audited source: CC0 1.0.

Vendored as:
- `assets/external/audio/rain_window_loop.wav`

### wind whoosh loop
Creator: SketchMan3  
Source: https://opengameart.org/content/wind-whoosh-loop  
Original file: `wind woosh loop.ogg`  
License: CC0 1.0.

Vendored as:
- `assets/external/audio/wind_loop.ogg`

Audited byte source for the three ambience files:
- https://github.com/jyau2810/me-again
- source commit: `840569e5e5535bfbfb5bc09f404b58f1eee9304a`
- provenance table: `docs/audio-licenses.md` in that repository.

## Runtime policy

External assets are deliberately bounded:
- high-detail imported buildings are shown only in premium quality tiers;
- near-camera animated GLB citizens are capped and disabled below High;
- bulk population remains MultiMesh/procedural;
- imported assets never replace the deterministic simulation state;
- low/Compatibility rendering continues to work without requiring high-detail scene density.

## Release validation

The 1.4 exported-game validation must produce `METROPOLE_ASSET_VALIDATION_OK`. Validation checks:
- every required external path exists after Godot import;
- every required PackedScene loads;
- at least four character GLBs expose AnimationPlayer clips;
- every required AudioStream loads;
- the ordinary gameplay/UI validation still succeeds afterwards.
