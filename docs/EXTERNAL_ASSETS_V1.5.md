# External Assets — METRÓPOLE ∞ 1.5

A 1.5 mantém os assets CC0 da 1.4 e adiciona materiais/HDRI da Poly Haven.

## Poly Haven — CC0

Canonical source and license:
- https://polyhaven.com/license
- https://polyhaven.com/a/urban_street_02
- https://polyhaven.com/a/asphalt_02
- https://polyhaven.com/a/concrete_pavement

Assets used:

### Urban Street 02
- type: HDRI;
- release build: 1K HDR;
- use: daytime sky/reflections/ambient lighting;
- license: CC0.

### Asphalt 02
- type: PBR texture;
- release build: 1K JPG maps;
- maps: diffuse, OpenGL normal, roughness;
- use: road material;
- license: CC0.

### Concrete Pavement
- type: PBR texture;
- release build: 1K JPG maps;
- maps: diffuse, OpenGL normal, roughness;
- use: sidewalks/ground;
- license: CC0.

The project uses direct Poly Haven CDN asset URLs; the files themselves are CC0. A build-time SHA-256 manifest is generated after download.

## Kenney — CC0 1.0

Retained from 1.4:
- City Kit (Commercial)
- City Kit (Industrial)
- City Kit (Roads)
- Car Kit
- Mini Characters
- Interface Sounds

Canonical source: https://kenney.nl/assets

## OpenGameArt — CC0

Retained:
- AMB Outside 1
- AMB Rain Loop 1

## Policy

- no unclear-license asset enters release;
- download failure fails CI;
- Low mode must not depend on expensive detailed assets;
- simulation state never depends on presentation assets;
- source/licensing documentation is maintained for publishing.
