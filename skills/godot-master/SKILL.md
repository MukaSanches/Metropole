# GODOT MASTER SKILL — METRÓPOLE ∞

Version: 1.0
Engine baseline: Godot 4.7.2 .NET
Primary language: C# / .NET 8
Primary target: Windows desktop
Secondary target: lower-end Windows fallback
Status: project-local durable engineering skill

## Purpose

This skill is the canonical Godot engineering reference for METRÓPOLE ∞. It exists so future work does not depend on rediscovering engine behavior, repeating avoidable mistakes, or applying features without considering architecture, performance, platform support, visual quality, and verification.

The goal is not to "use every Godot feature." The goal is to select the strongest combination of engine features for a deep life/city/business simulation while preserving deterministic simulation, responsiveness, scalability, and visual quality.

## Source precedence

When guidance conflicts, use this order:

1. Official Godot 4.7 documentation.
2. Godot 4.7.2 source/release notes.
3. Official Godot 4.7 demo projects.
4. Stable Godot 4.x documentation if the 4.7 page is unavailable.
5. Latest/unstable documentation only when explicitly marked as future-facing and verified against the pinned engine.
6. High-quality community material only as supplementary evidence.

Do not silently apply instructions from a different Godot major/minor version.

Primary references:
- https://docs.godotengine.org/en/4.7/
- https://docs.godotengine.org/en/4.7/about/list_of_features.html
- https://docs.godotengine.org/en/4.7/engine_details/architecture/internal_rendering_architecture.html
- https://github.com/godotengine/godot/releases/tag/4.7.2-stable
- https://github.com/godotengine/godot-demo-projects/releases

## Core architectural rule

Separate simulation truth from visual presentation.

The world state must remain plain deterministic data:
- citizens;
- households;
- companies;
- products;
- markets;
- districts;
- relationships;
- jobs;
- finances;
- events;
- time;
- weather state;
- policies and rules.

Godot nodes represent only what must be interactive, visible, audible, or editor-authored.

Never create one full SceneTree actor per simulated citizen just because that citizen exists.

Recommended model:

SimulationCore (plain C#)
    ↓ immutable/read-only view models/events
Presentation adapters
    ↓
Godot scene/UI/rendering/audio nodes

Visible-agent proxies may represent a small subset of the full simulation. The logical citizen continues to exist even if no Node exists for it.

## Determinism and time

Simulation progression must not depend on rendering FPS.

Use explicit simulation clocks and fixed logical ticks:
- hourly life tick;
- daily economy tick;
- weekly/monthly systems where appropriate;
- deterministic RNG seeded from world seed + tick + stable entity identity.

Rendering interpolation is visual only.

Never make money, population, demand, aging, production, or relationship outcomes depend directly on frame delta.

## Renderer strategy

### Primary desktop profile: Forward+

For a premium Windows presentation, Forward+ is the preferred visual profile because it exposes the largest rendering feature set:
- clustered lighting;
- advanced post-processing;
- SSAO;
- SSIL;
- SSR;
- volumetric fog;
- SDFGI/VoxelGI where appropriate;
- TAA/FSR2;
- CompositorEffects;
- compute-based capabilities.

### Fallback profile: Compatibility

Keep a lower-end graphics path for older hardware:
- simplified lighting;
- reduced particles;
- no effects unsupported by Compatibility;
- simplified materials;
- reduced visible-agent density;
- reduced shadow-casting lights;
- lower resolution/effect quality.

Do not design the premium renderer around the lowest common denominator. Instead implement quality profiles and degrade gracefully.

### Renderer verification

Any renderer switch requires actual visual/performance testing. Compatibility can differ visually from Forward+.

## Target visual architecture for METRÓPOLE ∞

The strongest long-term direction is a hybrid 2.5D city:

- orthographic Camera3D;
- real 3D building meshes, roads, vehicles, vegetation, props;
- 2D/Control UI in CanvasLayer;
- selected billboard/Sprite3D elements where cheaper than geometry;
- procedural city layout driven by the simulation;
- Forward+ default;
- Compatibility fallback;
- low-level instancing for repeated city assets.

Why:
- true lighting/shadows create significantly more depth than pure CanvasItem drawing;
- building facade material variation becomes inexpensive;
- camera zoom/pan/rotation becomes richer;
- weather can affect geometry/materials coherently;
- signs and branding can be rendered on buildings;
- vehicles and visible citizens can use shared meshes/animations;
- LOD/HLOD and MultiMesh provide a scaling path.

The current CanvasItem procedural renderer remains useful as a fallback, prototype renderer, minimap, and low graphics mode.

## 2D rendering

Use CanvasItem custom drawing when:
- geometry is simple;
- objects are numerous but do not need independent transforms/nodes;
- visuals can be generated mathematically;
- CPU-side procedural drawing is cheaper than SceneTree complexity.

Use TileMapLayer when authored/reusable tile data is more appropriate than arbitrary custom drawing.

Use CanvasModulate, PointLight2D, DirectionalLight2D and LightOccluder2D for true 2D lighting when staying in 2D.

Normal/specular maps can make 2D assets react to light with much stronger depth perception.

Forward+/Mobile support HDR 2D. Use HDR 2D selectively for emissive signage, vehicle lights, neon, emergency lights, and night-city glow.

Do not place gameplay HUD inside the glowing canvas layer unless desired.

## 3D rendering

Use 3D for the premium city layer.

Recommended baseline:
- orthographic camera for an isometric/management-game look;
- StandardMaterial3D/material shaders for buildings;
- DirectionalLight3D for sun/moon;
- Environment for sky, fog, tonemapping, glow, SSAO/SSIL;
- ReflectionProbe only where reflections materially improve the scene;
- decals for road markings, wear, dirt and localized surface detail when supported;
- LightmapGI or carefully selected dynamic GI when appropriate;
- avoid excessive shadow-casting local lights.

For city scenes:
- repeated building modules should be instanced;
- foliage/props should use MultiMesh where appropriate;
- distant districts should use HLOD or impostors;
- imported meshes should use automatic mesh LOD unless visual artifacts require exceptions;
- split large MultiMeshes spatially to preserve coarse culling.

## Lighting

Lighting should communicate time, weather, wealth, density and activity.

Day:
- one primary directional sun;
- sky/environment lighting;
- low reliance on local lights.

Night:
- local light budget by district and camera visibility;
- emissive windows;
- streetlights;
- signs;
- traffic headlights;
- warm/cool color contrast by land use.

Compatibility renderer does not scale well with many shadowed lights. Shadow-casting local lights must be sparse and carefully bounded.

Prefer unshadowed accent lights where shadows add little.

## Weather and atmosphere

Weather must be a complete presentation system, not only an overlay.

State affects:
- sky;
- light intensity;
- color temperature;
- road wetness;
- puddle/reflection response;
- fog visibility;
- particle density;
- traffic density/speed;
- citizen schedules;
- ambience;
- economic demand where logical.

Use GPUParticles2D/3D for rain, snow, dust, steam, smoke, leaves and localized ambience when visual density is high.

For premium Forward+:
- volumetric fog where visually justified;
- screen/reflection effects for wet surfaces;
- post-processing for exposure, saturation, contrast and atmosphere.

For fallback:
- screen-space overlays and cheaper particles.

## Shaders

Use shaders to move repetitive visual work from CPU to GPU.

Candidate shaders:
- wet road material;
- animated windows;
- billboard/sign flicker;
- district color grading;
- water/puddles;
- vegetation wind;
- cloud/fog layers;
- procedural facade variation;
- selection/highlight outline;
- heat haze/industrial distortion;
- day-night emissive response;
- subtle screen-space vignette/chromatic treatment only when stylistically justified.

Use CanvasItem shaders for 2D effects and Spatial shaders for 3D.

Use screen-reading shaders sparingly because they may require additional screen copies/back buffers.

Forward+/Mobile can use RenderingDevice/compute workflows. Compute shaders should only be introduced where measured workloads justify them.

## Post-processing

Premium Forward+ profile may use:
- tonemapping;
- glow;
- SSAO;
- SSIL;
- SSR on suitable opaque wet/reflective surfaces;
- fog;
- custom CompositorEffects only when the result is materially better than simpler methods.

Avoid stacking expensive effects without profiling.

Visual target: strong atmosphere without looking overprocessed.

## GPU particles

Prefer GPUParticles for large cosmetic populations:
- rain;
- snow;
- smoke;
- sparks;
- dust;
- birds at distance;
- chimney emissions;
- construction dust;
- leaves;
- ambient city debris.

Use collision only where it is visible enough to justify cost.

Particles are presentation, not simulation truth.

## Animation

Use AnimationPlayer for authored property animation and sequences.

Use AnimationTree when characters/vehicles need:
- state machines;
- blend spaces;
- layered animation;
- smooth transitions.

For thousands of repeated visual objects:
- avoid one AnimationPlayer per object;
- use MultiMesh + shader-driven animation, particles, or shared animation strategies.

Use Tween for lightweight UI and short procedural transitions.

## Camera

The camera is a major part of perceived quality.

Implement:
- smooth pan;
- smooth zoom;
- zoom-to-cursor;
- optional small orthographic rotation steps;
- cinematic focus on major events;
- district/ company follow;
- edge pan optional;
- keyboard/mouse/gamepad mappings;
- transition easing;
- bounds and zoom limits.

Never snap abruptly unless the user explicitly triggers a snap action.

Major events should offer focus, not forcibly steal the camera repeatedly.

## Visible citizens

Logical population can be thousands or more. Visible population should use proxy rendering.

Recommended tiers:
1. Near camera: full visible agent proxy with model/sprite, animation and local route.
2. Mid distance: instanced simplified pedestrian.
3. Far distance: aggregate crowd/particle/impostor or no individual rendering.

The proxy references the simulation citizen ID.

Never duplicate economy/life state inside the visual proxy.

Visible citizens should reflect actual state:
- work commute;
- school commute;
- shopping;
- leisure;
- home;
- weather response;
- district activity.

## Navigation

Use navigation only for visible/local actors that need believable path traversal.

Do not pathfind all simulated citizens continuously.

For a grid/isometric city, precomputed graph routing can often be cheaper for abstract simulation.

If NavigationServer/NavigationAgent is used:
- restrict to visible agents;
- partition regions;
- use navigation layers for actor classes;
- avoid unnecessary map rebuilds;
- profile avoidance.

NavigationServer2D is marked experimental in 4.7; avoid making the entire architecture depend on unstable low-level behavior unless tested.

## Physics

Do not use physics as the source of truth for economic/citizen simulation.

Use CharacterBody for visible controllable/moving agents only when collision response is needed.

Use RigidBody for physical props/effects, not ordinary simulation entities.

Use ray/shape queries for interaction and visibility instead of spawning unnecessary collision bodies.

Enable physics interpolation when relevant to smooth motion between physics ticks.

## UI

Use Control nodes and Containers for layout.

Avoid fixed coordinates for primary application/game UI.

Use:
- Theme resources;
- Theme type variations;
- consistent spacing tokens;
- typography scale;
- reusable cards;
- reusable chart controls;
- tooltips;
- proper focus navigation;
- responsive minimum sizes;
- keyboard/gamepad focus;
- localization-ready strings.

The game should remain usable at 1280×720, with 1600×900 and 1920×1080 as primary visual targets.

High-density business screens should prefer tabs/subpages over endlessly long vertical lists.

Recommended top-level UI:
- World;
- Life;
- People;
- Career;
- City;
- Market;
- Company;
- Finance;
- History/News.

## Information design

Every complex mechanic needs:
- current value;
- recent change;
- reason;
- consequence;
- action available.

Example:
“Brand awareness 42% (+3pp this month)”
Reason: marketing campaign + strong sales
Consequence: higher purchase probability
Action: increase/decrease marketing

Avoid presenting unexplained raw numbers.

## Business simulation

Company systems should be causally connected:

Brand
→ awareness
→ consideration
→ customer acquisition

Quality
→ satisfaction
→ loyalty
→ reputation

Price
→ conversion
→ margin
→ positioning

Salary/culture
→ morale
→ retention
→ productivity

R&D
→ innovation
→ quality/productivity/new products

Marketing
→ awareness
→ demand
→ diminishing returns

Competition
→ relative price/quality/brand
→ market share
→ strategic reactions

Finance
→ liquidity
→ debt service
→ investment capacity
→ risk

The UI must expose these causal chains.

## Life simulation

Life is a time-budget problem.

Actions consume hours and affect needs, skills, money, relationships and opportunities.

Core loops:
- sleep;
- work;
- commute;
- eat;
- study;
- exercise;
- socialize;
- leisure;
- family;
- household;
- purchases;
- healthcare;
- career;
- entrepreneurship.

Use schedules and priorities to automate ordinary days while preserving player agency.

Do not force the player to click every hour forever; support automation/routines.

## Social simulation

Relationships need:
- familiarity;
- affinity;
- trust;
- conflict;
- shared history;
- social graph;
- family ties;
- coworkers;
- rivals;
- mentorship.

Events should be generated from causes, not random flavor text alone.

Examples:
- coworker promoted;
- friend starts a company;
- former employee becomes competitor;
- spouse loses job;
- supplier relationship deteriorates.

## Audio

Audio is a major immersion multiplier.

Use audio buses for:
- Master;
- Music;
- UI;
- Ambience;
- Traffic;
- Weather;
- People;
- Industry;
- Notifications.

Use positional audio where it meaningfully tracks visible city sources.

Use bus effects for environmental space instead of baking reverb into every source.

Dynamic ambience should react to:
- time;
- weather;
- district;
- density;
- camera zoom;
- economic events.

At far zoom, use aggregated ambience rather than hundreds of positional emitters.

## Input

Use InputMap actions rather than hard-coded keys.

Support:
- keyboard;
- mouse;
- wheel zoom;
- gamepad where practical.

Godot 4.5+ uses SDL3 controller handling on Windows/macOS/Linux.

Allow remapping for important actions.

## Assets

Use source assets at appropriate fidelity.

For image imports:
- choose compression based on usage;
- avoid oversized textures;
- use VRAM compression for suitable 3D textures;
- preserve lossless where needed for UI/2D;
- use mipmaps for scaled world textures.

For 3D:
- prefer glTF workflow;
- allow automatic mesh LOD;
- check material complexity;
- standardize scale/orientation;
- build modular city kits.

For audio:
- avoid unnecessary 24-bit/high sample-rate data;
- mono is appropriate for many positional effects;
- use audio buses for reverb/effects.

## Resource loading

Large scenes/assets should not block the UI unnecessarily.

Use threaded/background loading for large resources and transitions.

Preload critical lightweight resources.

Do not dynamically load the same resource repeatedly per frame.

## Performance

Measure before optimizing.

Use Godot profiler/monitors and platform tools.

Track:
- frame time;
- draw calls;
- primitives;
- object/node count;
- script time;
- physics time;
- rendering time;
- memory;
- stutter;
- simulation tick time.

Targets for METRÓPOLE should be explicit per quality profile.

Suggested performance budgets to validate, not assume:
- 60 FPS target on recommended desktop at 1080p;
- 30 FPS acceptable fallback on minimum profile;
- no frame hitch above perceptible threshold during normal camera travel;
- economic tick must be independent of render frame time;
- visual population density scales with profile.

## SceneTree scaling

When object counts become high:
- stop adding nodes blindly;
- use MultiMesh;
- use RenderingServer directly;
- use pooled proxies;
- use spatial chunks;
- use visibility ranges;
- use HLOD;
- use automatic mesh LOD;
- split MultiMeshes by city chunk;
- consider GDExtension/C++ only for a measured hotspot that C# and engine servers cannot meet.

The scene system is optional above Godot servers; low-level servers can bypass Node overhead for extreme counts.

## C# performance

Use .NET collections for internal simulation unless Godot collections are required by the engine API. Godot collections cross the C#/C++ boundary and marshaling can be expensive inside tight loops.

Avoid per-frame allocations in hot rendering paths.

Cache references/resources.

Avoid LINQ in measured per-frame hotspots; it is fine in low-frequency code when clarity is more valuable.

Use structs carefully and avoid accidental copies of large values.

Profile before introducing unsafe/native code.

## Threads

Simulation work can use worker threads if:
- state ownership is clear;
- results are published safely;
- engine SceneTree APIs are not called from unsafe threads.

Use thread-safe server APIs where documented.

Do not mutate ordinary Godot nodes from background threads unless explicitly supported.

## MultiMesh and low-level rendering

Use MultiMesh for thousands of repeated meshes.

Advantages:
- very low draw-call count;
- per-instance transform/color/custom data;
- shader-driven variation.

Constraint:
- individual instances are not independently culled within one MultiMesh.

Therefore partition by district/chunk.

For very high counts, use RenderingServer and multimesh buffer updates.

## LOD/HLOD/occlusion

Use automatic mesh LOD for imported meshes.

Use visibility ranges/HLOD for:
- building groups;
- district clusters;
- trees;
- props;
- labels;
- particles.

Use occlusion culling only where city geometry provides useful blockers. Open top-down/isometric scenes often benefit more from LOD/HLOD than CPU occlusion culling.

Measure the result.

## Shader/pipeline stutter

Forward+/Mobile use modern pipeline compilation.

To reduce first-use stutter:
- ensure shaders/material variants are discoverable during loading;
- preload representative effects;
- avoid generating unpredictable material variants at the moment they first appear;
- enable shader baking during export when appropriate;
- run a warmup scene for known materials/effects.

## SubViewports

Use SubViewport strategically for:
- live company signage;
- minimaps;
- security-camera style screens;
- dynamic billboards;
- UI rendered into world objects;
- thumbnails;
- composited effects.

Do not create one live SubViewport per building.

Pool or update only visible/important signs.

## Editor tooling

Build EditorPlugin/tools when content production becomes repetitive.

Candidates:
- district generator;
- road/building placement tools;
- business facade preview;
- material variation generator;
- asset validator;
- simulation seed inspector;
- citizen/company debugger;
- performance overlay;
- event timeline viewer.

Internal tools are part of production quality.

## GDExtension

Use GDExtension only when justified by measurement or external native libraries.

Strong candidates in the future:
- extremely high-performance crowd transform generation;
- heavy path graph computation;
- native data processing;
- integration with a specialized library.

Do not migrate ordinary gameplay code to C++ preemptively.

## Multiplayer/networking

Not currently required for core METRÓPOLE.

If introduced:
- simulation authority must be explicit;
- deterministic logic is an advantage;
- use Godot high-level multiplayer/RPC only after defining authoritative state and reconciliation.

Do not let networking contaminate offline core architecture prematurely.

## Accessibility

Support:
- scalable UI;
- keyboard focus;
- readable contrast;
- icons plus text;
- color-independent status signals;
- reduced motion option;
- effects intensity;
- font scaling;
- audio sliders/subtitles if voice is introduced.

## Localization

All player-visible strings should be externalizable.

Do not concatenate translated grammar where pluralization/ordering varies.

Support Portuguese first but keep architecture translation-ready.

## Saving/versioning

Save data is a contract.

Every schema change requires:
- version field;
- migration path or explicit incompatibility;
- backup;
- validation;
- corrupted-save recovery where possible.

Never silently overwrite a valid save with an invalid state.

## Export

Windows release checklist:
- release export;
- correct product version;
- icon;
- executable metadata;
- installer;
- SHA-256;
- smoke launch exported EXE;
- install test;
- smoke launch installed EXE;
- code signing when a real persistent certificate is configured.

Do not claim signing unless the artifact is actually signed and verified.

## Validation gates

Every meaningful release must pass:

1. .NET build.
2. Simulation unit/integration tests.
3. Multi-seed long-run simulation.
4. Save/load validation.
5. Godot project import.
6. Godot C# build.
7. In-engine UI/gameplay validation.
8. Export Windows.
9. Exported EXE smoke test.
10. Installer build.
11. Silent install.
12. Installed EXE smoke test.
13. Checksum generation.

Future visual pipeline should add:
- deterministic screenshot capture for key scenes;
- image-diff thresholds;
- frame-time benchmark scenes;
- GPU/CPU profile snapshots;
- quality-profile validation.

## Visual quality doctrine

A premium simulation game should not look busy everywhere.

Use:
- hierarchy;
- contrast;
- motion only where meaningful;
- coherent material palette;
- atmospheric depth;
- readable silhouettes;
- controlled glow;
- restrained particles;
- visible cause/effect.

Avoid:
- excessive bloom;
- random neon;
- motion on every element;
- tiny unreadable labels;
- UI that hides simulation feedback;
- realistic effects that conflict with the stylized art direction.

## METRÓPOLE visual destination

The target is a stylized premium 2.5D city/business/life simulation:

- orthographic 3D city;
- high-quality modular buildings;
- animated traffic;
- visible pedestrians tied to simulation;
- day/night;
- weather affecting materials and behavior;
- windows and signage that react to business activity;
- district identity;
- construction and bankruptcy visible in the city;
- company logos/signage in-world;
- smooth camera;
- cinematic event focus;
- premium responsive UI;
- dynamic city audio;
- clear visual cause/effect for economy and life.

The player should be able to understand the city by looking at it before opening a spreadsheet.

## Non-negotiable engineering rules

- No fake functionality.
- No fake test claims.
- No destructive rewrite of working systems without a migration reason.
- No engine feature added only because it exists.
- No full agent Node per logical citizen at scale.
- No economy tied to FPS.
- No hidden money creation unless explicitly modeled and explained.
- No renderer-dependent feature without fallback or compatibility policy.
- No major visual feature without a measurable performance budget.
- No release without exported/install smoke validation.
