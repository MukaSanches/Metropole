# External CC0 Assets — METRÓPOLE ∞ 1.4

All third-party assets introduced by the 1.4 visual/audio pass are restricted to public-domain/CC0 sources.

The build uses `tools/fetch_cc0_assets.ps1` to retrieve pinned files. Large binary assets are intentionally not duplicated in Git history. The script pins GitHub mirror commits for reproducibility and produces `assets/external/SHA256SUMS.txt` during the build.

## Kenney — CC0 1.0

Official source pages:
- City Kit (Commercial): https://kenney.nl/assets/city-kit-commercial
- City Kit (Industrial): https://kenney.nl/assets/city-kit-industrial
- City Kit (Roads): https://kenney.nl/assets/city-kit-roads
- Car Kit: https://kenney.nl/assets/car-kit
- Mini Characters: https://kenney.nl/assets/mini-characters
- Interface Sounds: https://kenney.nl/assets/interface-sounds

License: Creative Commons CC0 1.0 Universal. Attribution is not required, but the project keeps source records for auditability.

Pinned mirrors used only as immutable binary transport:
- city models: MMqd/godot-screenspace-projection @ d00f54f4acd328bc2162656a09f4b78a9a1e6364
- car models: ruiguitos/horde-breaker @ 697e73f478286d0c55d6caf3df4db421a625137c
- Mini Characters: AkiraNim/CLTCrossing @ 6fe4cd6dcb6fbfa4267d3b9971c0968e0fe375b6
- interface audio: Calinou/kenney-interface-sounds @ 4596a49eaf5a533948d49a47467f606bcdea70ff

The upstream Kenney pages, not the mirrors, are the canonical license/source evidence.

## OpenGameArt ambience — CC0

- AMB Outside 1 — Kresiek The Furry:
  https://opengameart.org/content/amb-outside-1
  License: CC0.
- AMB Rain Loop 1 — Kresiek The Furry:
  https://opengameart.org/content/amb-rain-loop-1
  License: CC0.

## Usage policy

- No asset with unclear or attribution-conflicting licensing enters the release build.
- External assets are presentation only; simulation logic never depends on their availability.
- Low graphics mode can fall back to procedural geometry and does not require animated character/model detail.
- If an external download becomes unavailable, CI must fail instead of silently replacing it with an unreviewed file.
