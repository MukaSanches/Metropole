param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot "..\src\Metropole.Game")
)

$ErrorActionPreference = "Stop"
$assetsRoot = Join-Path $ProjectRoot "assets\external"
New-Item -ItemType Directory -Force -Path $assetsRoot | Out-Null

function Get-PinnedAsset {
    param(
        [Parameter(Mandatory=$true)][string]$Url,
        [Parameter(Mandatory=$true)][string]$RelativePath,
        [int]$MinBytes = 128
    )
    $target = Join-Path $assetsRoot $RelativePath
    $dir = Split-Path $target -Parent
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    if (Test-Path $target) {
        $existing = Get-Item $target
        if ($existing.Length -ge $MinBytes) {
            Write-Host "asset cached: $RelativePath ($($existing.Length) bytes)"
            return
        }
        Remove-Item $target -Force
    }

    Write-Host "asset download: $RelativePath"
    Invoke-WebRequest -Uri $Url -OutFile $target -UseBasicParsing
    $file = Get-Item $target
    if ($file.Length -lt $MinBytes) {
        throw "Downloaded asset is unexpectedly small: $RelativePath ($($file.Length) bytes)"
    }
}

$cityCommit = "d00f54f4acd328bc2162656a09f4b78a9a1e6364"
$carCommit = "697e73f478286d0c55d6caf3df4db421a625137c"
$characterCommit = "6fe4cd6dcb6fbfa4267d3b9971c0968e0fe375b6"
$audioCommit = "4596a49eaf5a533948d49a47467f606bcdea70ff"
$kaykitCityCommit = "63976910ca04d16f0fc531b9c614244be8128713"

$cityBase = "https://raw.githubusercontent.com/MMqd/godot-screenspace-projection/$cityCommit"
$carBase = "https://raw.githubusercontent.com/ruiguitos/horde-breaker/$carCommit"
$characterBase = "https://raw.githubusercontent.com/AkiraNim/CLTCrossing/$characterCommit"
$audioBase = "https://raw.githubusercontent.com/Calinou/kenney-interface-sounds/$audioCommit"
$expandedBase = "https://raw.githubusercontent.com/ruiguitos/horde-breaker/$carCommit"
$kaykitCityBase = "https://raw.githubusercontent.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0/$kaykitCityCommit/addons/kaykit_city_builder_bits/Assets/gltf"

$assets = @(
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-a.glb", "kenney\city\commercial\building-commercial-a.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-e.glb", "kenney\city\commercial\building-commercial-e.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-h.glb", "kenney\city\commercial\building-commercial-h.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-skyscraper-a.glb", "kenney\city\commercial\building-skyscraper-a.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-industrial_1.0/building-c.glb", "kenney\city\industrial\building-industrial-c.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-industrial_1.0/building-m.glb", "kenney\city\industrial\building-industrial-m.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-roads/road-straight.glb", "kenney\city\roads\road-straight.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-roads/light-curved.glb", "kenney\city\roads\light-curved.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/Textures/colormap.png", "kenney\city\commercial\Textures\colormap.png", 1000),
    @("$cityBase/assets/kenney_city-kit-industrial_1.0/Textures/colormap.png", "kenney\city\industrial\Textures\colormap.png", 1000),
    @("$cityBase/assets/kenney_city-kit-roads/Textures/colormap.png", "kenney\city\roads\Textures\colormap.png", 1000),

    @("$carBase/assets/models/kenney_car_kit/sedan.glb", "kenney\vehicles\sedan.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/taxi.glb", "kenney\vehicles\taxi.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/delivery.glb", "kenney\vehicles\delivery.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/van.glb", "kenney\vehicles\van.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/police.glb", "kenney\vehicles\police.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/firetruck.glb", "kenney\vehicles\firetruck.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/Textures/colormap.png", "kenney\vehicles\Textures\colormap.png", 1000),

    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-female-a.glb", "kenney\characters\female-a.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-female-b.glb", "kenney\characters\female-b.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-female-c.glb", "kenney\characters\female-c.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-male-a.glb", "kenney\characters\male-a.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-male-b.glb", "kenney\characters\male-b.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-male-c.glb", "kenney\characters\male-c.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/wheelchair.glb", "kenney\characters\wheelchair.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/Textures/colormap.png", "kenney\characters\Textures\colormap.png", 1000),

    @("$audioBase/addons/kenney_interface_sounds/click_001.wav", "kenney\audio\ui-click.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/select_001.wav", "kenney\audio\ui-select.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/confirmation_001.wav", "kenney\audio\ui-confirm.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/error_001.wav", "kenney\audio\ui-error.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/open_001.wav", "kenney\audio\ui-open.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/back_001.wav", "kenney\audio\ui-back.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/switch_001.wav", "kenney\audio\ui-switch.wav", 1000),
    @("$audioBase/addons/kenney_interface_sounds/scroll_001.wav", "kenney\audio\ui-scroll.wav", 1000),

    @("https://opengameart.org/sites/default/files/amb_outdoor1_loop.ogg", "opengameart\audio\city-outdoor.ogg", 20000),
    @("https://opengameart.org/sites/default/files/amb_rain_loop_1.ogg", "opengameart\audio\rain-loop.ogg", 20000),

    @("https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/1k/urban_street_02_1k.hdr", "polyhaven\hdri\urban_street_02_1k.hdr", 500000),
    @("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/asphalt_02/asphalt_02_diff_1k.jpg", "polyhaven\textures\asphalt_02_diff_1k.jpg", 100000),
    @("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/asphalt_02/asphalt_02_nor_gl_1k.jpg", "polyhaven\textures\asphalt_02_nor_gl_1k.jpg", 100000),
    @("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/asphalt_02/asphalt_02_rough_1k.jpg", "polyhaven\textures\asphalt_02_rough_1k.jpg", 50000),
    @("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/concrete_pavement/concrete_pavement_diff_1k.jpg", "polyhaven\textures\concrete_pavement_diff_1k.jpg", 100000),
    @("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/concrete_pavement/concrete_pavement_nor_gl_1k.jpg", "polyhaven\textures\concrete_pavement_nor_gl_1k.jpg", 100000),
    @("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/concrete_pavement/concrete_pavement_rough_1k.jpg", "polyhaven\textures\concrete_pavement_rough_1k.jpg", 50000)
)

# V1.7 Visual Leap: expand coherent urban libraries while keeping every source pinned.
$commercialNames = @(
    "building-b","building-c","building-d","building-f","building-g","building-i","building-j",
    "building-k","building-l","building-m","building-n","building-skyscraper-b","building-skyscraper-c",
    "building-skyscraper-d","building-skyscraper-e","detail-awning","detail-awning-wide",
    "detail-parasol-a","detail-parasol-b"
)
foreach ($name in $commercialNames) {
    $assets += ,@("$expandedBase/assets/models/kenney_city_commercial/$name.glb", "kenney\city\commercial\$name.glb", 1000)
}

$industrialNames = @(
    "building-a","building-b","building-d","building-e","building-f","building-g","building-h","building-i",
    "building-j","building-k","building-l","building-n","building-o","building-p","building-q","building-r",
    "building-s","building-t","chimney-basic","chimney-medium","chimney-small","detail-tank"
)
foreach ($name in $industrialNames) {
    $assets += ,@("$expandedBase/assets/models/kenney_city_industrial/$name.glb", "kenney\city\industrial\$name.glb", 1000)
}

$vehicleNames = @(
    "ambulance","garbage-truck","hatchback-sports","sedan-sports","suv-luxury","suv",
    "truck-flat","truck","tractor","delivery-flat"
)
foreach ($name in $vehicleNames) {
    $assets += ,@("$expandedBase/assets/models/kenney_car_kit/$name.glb", "kenney\vehicles\$name.glb", 1000)
}

$factoryNames = @(
    "crane","crane-lift","machine","machine-fortified","robot-arm-a","robot-arm-b",
    "conveyor","conveyor-corner","hopper-round","screen-panel-flat","pipe-large","pipe-large-curve"
)
$assets += ,@("$expandedBase/assets/models/kenney_factory_kit/Textures/colormap.png", "kenney\factory\Textures\colormap.png", 1000)
foreach ($name in $factoryNames) {
    $assets += ,@("$expandedBase/assets/models/kenney_factory_kit/$name.glb", "kenney\factory\$name.glb", 1000)
}

# KayKit City Builder Bits (official repository, CC0): modular residences and street furniture.
$kaykitNames = @(
    "building_A","building_B","building_C","building_D","building_E","building_F","building_G","building_H",
    "bench","bush","dumpster","firehydrant","streetlight","trafficlight_A","trafficlight_B","trafficlight_C",
    "trash_A","trash_B","watertower"
)
$assets += ,@("$kaykitCityBase/citybits_texture.png", "kaykit\city\citybits_texture.png", 1000)
foreach ($name in $kaykitNames) {
    $assets += ,@("$kaykitCityBase/$name.gltf", "kaykit\city\$name.gltf", 500)
    $assets += ,@("$kaykitCityBase/$name.bin", "kaykit\city\$name.bin", 128)
}

foreach ($asset in $assets) {
    Get-PinnedAsset -Url $asset[0] -RelativePath $asset[1] -MinBytes $asset[2]
}

$manifest = @()
Get-ChildItem $assetsRoot -File -Recurse | Sort-Object FullName | ForEach-Object {
    $hash = Get-FileHash $_.FullName -Algorithm SHA256
    $relative = $_.FullName.Substring($assetsRoot.Length + 1).Replace("\","/")
    $manifest += "$($hash.Hash.ToLower())  $relative"
}
$manifest | Set-Content (Join-Path $assetsRoot "SHA256SUMS.txt") -Encoding UTF8

Write-Host "CC0 asset acquisition complete: $($assets.Count) source files (Kenney, KayKit, OpenGameArt, Poly Haven)."
