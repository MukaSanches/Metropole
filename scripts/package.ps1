param(
    [string]$GodotVersion = "4.7.2",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$tag = "$GodotVersion-stable"
$cache = Join-Path $root ".toolcache"
$godotZip = Join-Path $cache "godot-mono.zip"
$templatesZip = Join-Path $cache "templates.tpz"
$godotDir = Join-Path $cache "godot"
$templateExtract = Join-Path $cache "template-extract"
$build = Join-Path $root "build"

New-Item -ItemType Directory -Force -Path $cache,$godotDir,$templateExtract,$build | Out-Null

$godotUrl = "https://github.com/godotengine/godot/releases/download/$tag/Godot_v$($tag)_mono_win64.zip"
$templatesUrl = "https://github.com/godotengine/godot/releases/download/$tag/Godot_v$($tag)_mono_export_templates.tpz"

if (!(Test-Path $godotZip)) { Invoke-WebRequest -Uri $godotUrl -OutFile $godotZip }
if (!(Test-Path $templatesZip)) { Invoke-WebRequest -Uri $templatesUrl -OutFile $templatesZip }

Remove-Item -Recurse -Force $godotDir -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $templateExtract -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $godotDir,$templateExtract | Out-Null
Expand-Archive -Path $godotZip -DestinationPath $godotDir -Force
Expand-Archive -Path $templatesZip -DestinationPath $templateExtract -Force

$godot = Get-ChildItem $godotDir -Recurse -Filter "*mono_win64.exe" | Select-Object -First 1
if (!$godot) { throw "Executável Godot .NET não encontrado." }

$templateTarget = Join-Path $env:APPDATA "Godot\export_templates\$GodotVersion.stable.mono"
New-Item -ItemType Directory -Force -Path $templateTarget | Out-Null
Copy-Item (Join-Path $templateExtract "templates\*") $templateTarget -Recurse -Force

dotnet build "src/Metropole.Sim/Metropole.Sim.csproj" -c $Configuration
dotnet run --project "tests/Metropole.SimTests/Metropole.SimTests.csproj" -c $Configuration
dotnet build "src/Metropole.Game/Metropole.Game.csproj" -c $Configuration

& $godot.FullName --headless --path "src/Metropole.Game" --editor --quit
& $godot.FullName --headless --path "src/Metropole.Game" --export-release "Windows" (Join-Path $build "Metropole.exe")
if ($LASTEXITCODE -ne 0) { throw "Export Godot falhou com código $LASTEXITCODE." }
if (!(Test-Path (Join-Path $build "Metropole.exe"))) { throw "Metropole.exe não foi produzido." }

Get-FileHash (Join-Path $build "Metropole.exe") -Algorithm SHA256 | Format-List
