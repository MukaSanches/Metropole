# METRÓPOLE ∞ — GODOT VISUAL & MECHANICS ROADMAP

Version target: 1.3+
Prepared after a broad review of Godot 4.7 documentation, 4.7.2 release material, official demos, rendering architecture, performance guidance, animation, navigation, UI, audio, shaders, particles, resource/import/export systems and extension APIs.

## Primary conclusion

The current 1.2 custom CanvasItem city is an effective lightweight renderer, but it now limits the visual ceiling.

The next major leap should not be “more circles and polygons.” It should move the premium city presentation to a hybrid 2.5D/3D architecture while keeping the deterministic C# simulation untouched.

## Proposed target look

### Camera
- orthographic/isometric 3D camera;
- smooth zoom-to-cursor;
- fluid pan;
- optional discrete rotation;
- cinematic framing of bankruptcies, openings, major construction and life milestones;
- depth-aware selection/highlight.

### City
- modular 3D buildings with procedural facade/material variants;
- district-specific architecture;
- real roads, sidewalks, intersections and street furniture;
- trees and props via MultiMesh;
- vehicles with instanced models;
- close pedestrians with simple skeletal or sprite-based animation;
- distant population represented by cheaper proxies;
- construction sites visibly progress;
- closed companies visibly shutter;
- premium brands visually upgrade storefronts;
- neglected/crisis districts visibly degrade.

### Lighting
Day:
- sun DirectionalLight3D;
- environment sky;
- district/material response.

Night:
- emissive windows;
- streetlights;
- store signs;
- vehicle lights;
- wealth-dependent light density;
- limited shadow-casting local lights.

### Weather
- GPU rain;
- wet-road shader;
- puddle/reflection treatment;
- fog;
- clouds;
- storm exposure/color change;
- pedestrians and traffic react to weather;
- weather changes consumption patterns.

### Post-processing
Forward+ high profile:
- tonemapping;
- glow;
- SSAO;
- SSIL;
- selected SSR;
- volumetric fog;
- optional custom compositor effects.

Low profile:
- Compatibility renderer;
- simplified fog/glow;
- no advanced screen-space effects;
- lower particle and visible-agent counts.

### Branding in the world
- company color palette;
- procedural logo resource;
- storefront signage;
- building flags/billboards;
- product ads;
- headquarters upgrade;
- logo visible on delivery vehicles;
- bankrupt company signage removed/covered.

Dynamic signage should use pooled SubViewports or generated textures, never one expensive live viewport per business.

## Mechanical expansion made visible

### Business

Add:
- incorporation types;
- ownership percentages;
- partners;
- founders;
- board/management roles;
- departments;
- detailed payroll;
- benefits;
- hiring funnel;
- employee skills;
- performance reviews;
- promotions;
- turnover;
- workplace culture;
- offices/facilities;
- store/branch locations;
- capacity;
- suppliers;
- contracts;
- inventory;
- logistics;
- SKU/product lines;
- R&D projects;
- patents/IP abstraction;
- customer segments;
- marketing channels;
- brand campaigns;
- pricing per product;
- financing rounds/loans;
- dividends;
- cash-flow statement;
- balance-sheet style state;
- taxes;
- acquisitions;
- mergers;
- sale of company;
- bankruptcy restructuring;
- franchises;
- subsidiaries;
- holdings.

Visual response:
- hiring increases commute to the workplace;
- expansion changes the building;
- high sales increase deliveries;
- stock problems reduce store activity;
- marketing appears as signage/ads;
- bankruptcy shutters the location;
- acquisition changes branding.

### Life

Add:
- household budget;
- rent/mortgage;
- homes;
- possessions;
- transportation;
- healthcare;
- illness/injury abstraction;
- education tracks;
- certifications;
- job applications/interviews;
- promotions;
- hobbies;
- friendships;
- romantic stages;
- marriage;
- children;
- family finances;
- inheritance;
- retirement;
- goals/aspirations;
- memories;
- personality evolution.

Visual response:
- home location visible;
- commute visible;
- leisure venues populate at relevant times;
- wealth affects residence/transport;
- weather changes routines;
- family milestones enter the news/timeline.

### Citizens

Add:
- social graph;
- preferences;
- consumption brands;
- workplace satisfaction;
- commute tolerance;
- housing preference;
- health;
- education goals;
- entrepreneurial propensity;
- political/civic opinions only if later needed, kept descriptive and systemic;
- memory of employers/relationships;
- migration into/out of city;
- generational mobility.

Visual response:
- crowd density depends on actual schedules;
- shopping districts peak at certain hours;
- schools empty/fill;
- workplaces produce commute waves;
- unemployment changes daytime pedestrian patterns.

### City economy

Add:
- commercial rent;
- residential rent;
- land value;
- wages by sector;
- inflation;
- interest rate abstraction;
- credit conditions;
- demographics;
- migration;
- sector cycles;
- infrastructure capacity;
- utilities abstraction;
- transport accessibility;
- district desirability.

Visual response:
- cranes in growth areas;
- vacancies in recession;
- traffic pressure near employment centers;
- store turnover;
- district lighting/maintenance changes.

## Godot implementation plan

### Phase 1 — Render architecture
- add Forward+ premium renderer profile;
- keep Compatibility fallback;
- build Camera3D orthographic controller;
- create a City3DView separate from simulation;
- build chunk/district abstraction;
- preserve old CanvasItem view as fallback/minimap.

### Phase 2 — Asset kit
- modular road pieces;
- sidewalks;
- 6–10 base building families;
- material variants;
- props;
- trees;
- cars;
- pedestrian proxy assets;
- business signage system.

Use glTF and automatic mesh LOD.

### Phase 3 — GPU scaling
- MultiMesh vegetation;
- MultiMesh props;
- MultiMesh distant pedestrians;
- MultiMesh traffic where possible;
- chunk-based visibility;
- HLOD;
- automatic mesh LOD;
- quality-profile density scaling.

### Phase 4 — Atmosphere
- Environment;
- day/night lighting;
- HDR/glow;
- fog;
- rain/snow particle presets;
- wet surfaces;
- audio buses and dynamic ambience.

### Phase 5 — Mechanical visibility
- citizen visual proxy manager;
- traffic proxy manager;
- business facade state;
- construction/closure states;
- district prosperity/degradation indicators;
- event focus/camera.

### Phase 6 — UI overhaul
- central Theme resource;
- reusable components;
- tabs for company subsystems;
- charts for finance/market/HR;
- news ticker/timeline;
- tooltips explaining cause/effect;
- keyboard/gamepad focus.

### Phase 7 — Audio
- city ambience;
- traffic bed;
- rain;
- office/store ambience;
- industrial ambience;
- day/night sound changes;
- bus effects;
- notifications.

### Phase 8 — QA/performance
- visual benchmark scene;
- screenshot regression capture;
- profiler budgets;
- multi-seed simulation;
- quality presets;
- minimum/recommended machine checks;
- exported EXE validation.

## Quality profiles

### Ultra
- Forward+;
- full post-processing;
- highest visible population;
- SSR where useful;
- volumetric fog;
- high particle density;
- high shadow quality;
- dense props.

### High
- Forward+;
- SSAO/SSIL;
- restrained SSR;
- medium-high particles;
- high visible population.

### Medium
- Forward+ or Mobile;
- reduced screen-space effects;
- lower shadows;
- lower visible-agent counts.

### Low
- Compatibility;
- simplified city material;
- no volumetric effects;
- reduced particles;
- aggressive HLOD;
- low visible-agent density.

## Performance strategy

Do not use one Node per simulation entity.

Targets:
- render only visible proxies;
- chunk city by district/road block;
- split MultiMeshes spatially;
- use LOD/HLOD;
- pool effect nodes;
- update distant agents at lower rates;
- use shader animation for repeated low-detail motion;
- background-load larger resources;
- profile before GDExtension.

## What the player should feel after this transition

At 7:30:
- residential streets begin filling;
- trains/buses/traffic intensify;
- office districts light up;
- stores begin opening;
- citizens visually commute.

At noon:
- commercial areas become crowded;
- restaurants receive more visitors;
- delivery traffic increases.

During rain:
- roads become wet/reflective;
- umbrellas/rain proxies appear where supported;
- pedestrians decrease;
- traffic slows;
- ambience changes;
- selected product demand changes.

When a company grows:
- brand recognition rises;
- signage improves;
- delivery frequency rises;
- staff presence increases;
- branch/headquarters can visually upgrade.

When a company fails:
- activity declines;
- workers disappear;
- deliveries stop;
- storefront closes;
- sign changes/removes;
- unemployment and nearby commerce react.

When the player becomes wealthy:
- home/business locations improve;
- visible lifestyle changes;
- network/status opportunities change;
- the city reflects the player's economic footprint.

The intended result is not a spreadsheet with a city background. It is a city whose visuals are an observable projection of the simulation.
