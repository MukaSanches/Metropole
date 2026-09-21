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

$cityBase = "https://raw.githubusercontent.com/MMqd/godot-screenspace-projection/$cityCommit"
$carBase = "https://raw.githubusercontent.com/ruiguitos/horde-breaker/$carCommit"
$characterBase = "https://raw.githubusercontent.com/AkiraNim/CLTCrossing/$characterCommit"
$audioBase = "https://raw.githubusercontent.com/Calinou/kenney-interface-sounds/$audioCommit"

$assets = @(
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-a.glb", "kenney\city\building-commercial-a.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-e.glb", "kenney\city\building-commercial-e.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-h.glb", "kenney\city\building-commercial-h.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-commercial_2.1/building-skyscraper-a.glb", "kenney\city\building-skyscraper-a.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-industrial_1.0/building-c.glb", "kenney\city\building-industrial-c.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-industrial_1.0/building-m.glb", "kenney\city\building-industrial-m.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-roads/road-straight.glb", "kenney\city\road-straight.glb", 1000),
    @("$cityBase/assets/kenney_city-kit-roads/light-curved.glb", "kenney\city\light-curved.glb", 1000),

    @("$carBase/assets/models/kenney_car_kit/sedan.glb", "kenney\vehicles\sedan.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/taxi.glb", "kenney\vehicles\taxi.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/delivery.glb", "kenney\vehicles\delivery.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/van.glb", "kenney\vehicles\van.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/police.glb", "kenney\vehicles\police.glb", 1000),
    @("$carBase/assets/models/kenney_car_kit/firetruck.glb", "kenney\vehicles\firetruck.glb", 1000),

    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-female-a.glb", "kenney\characters\female-a.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-female-b.glb", "kenney\characters\female-b.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-female-c.glb", "kenney\characters\female-c.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-male-a.glb", "kenney\characters\male-a.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-male-b.glb", "kenney\characters\male-b.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/character-male-c.glb", "kenney\characters\male-c.glb", 1000),
    @("$characterBase/CltCrossingv2/assets/kenney_mini-characters/wheelchair.glb", "kenney\characters\wheelchair.glb", 1000),

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

Write-Host "CC0 asset acquisition complete: $($assets.Count) source files (Kenney, OpenGameArt, Poly Haven)."
