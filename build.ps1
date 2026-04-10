<#
.SYNOPSIS
  Build the ClassicMode mod for Slay the Spire 2.

.DESCRIPTION
  Standalone build script. Needs two things from your environment:
    1. A Slay the Spire 2 install (for the game DLLs it compiles against)
    2. An unpacked copy of Slay the Spire 1 assets (for portraits + loc)

  Resolution order for each path:
    1. -GameDir / -Sts1Dir command-line parameter
    2. STS2_GAME_DIR / STS1_UNPACKED_DIR environment variable
    3. (game only) auto-detect the default Steam install

  Outputs land in .\build\ClassicMode\ along with zipped release archives.

.PARAMETER GameDir
  Path to your Slay the Spire 2 installation.
  Must contain data_sts2_windows_x86_64\sts2.dll.

.PARAMETER Sts1Dir
  Path to an unpacked Slay the Spire 1 assets root (i.e. a directory that
  contains images\1024Portraits\, images\relics\, localization\eng\cards.json, ...).
  Only required when building assets (card portraits + localization).

.PARAMETER SkipAssets
  Skip the prepare_assets.py step entirely. Useful when you only want to
  iterate on C# code and already have a populated _pck_src.

.EXAMPLE
  .\build.ps1

.EXAMPLE
  $env:STS2_GAME_DIR = "D:\Steam\steamapps\common\Slay the Spire 2"
  $env:STS1_UNPACKED_DIR = "D:\sts1_unpacked"
  .\build.ps1

.EXAMPLE
  .\build.ps1 -GameDir "D:\Steam\steamapps\common\Slay the Spire 2" -SkipAssets
#>
param(
  [string]$GameDir,
  [string]$Sts1Dir,
  [switch]$SkipAssets
)

$ErrorActionPreference = "Stop"

$modName = "ClassicMode"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# ---------------------------------------------------------------------------
# Resolve STS2 game dir (for referenced DLLs)
# ---------------------------------------------------------------------------
if (-not $GameDir) { $GameDir = $env:STS2_GAME_DIR }
if (-not $GameDir) {
  $steamDefaults = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    "D:\Steam\steamapps\common\Slay the Spire 2",
    "D:\software\steam\steamapps\common\Slay the Spire 2",
    "E:\Steam\steamapps\common\Slay the Spire 2"
  )
  foreach ($c in $steamDefaults) {
    if (Test-Path (Join-Path $c "data_sts2_windows_x86_64\sts2.dll")) { $GameDir = $c; break }
  }
}
if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir "data_sts2_windows_x86_64\sts2.dll"))) {
  throw @"
Could not locate Slay the Spire 2 install.
  Provide it with one of:
    -GameDir "C:\path\to\Slay the Spire 2"
    `$env:STS2_GAME_DIR = "C:\path\to\Slay the Spire 2"
  The directory must contain data_sts2_windows_x86_64\sts2.dll.
"@
}
$GameDir = (Resolve-Path $GameDir).Path
$env:STS2_GAME_DIR = $GameDir   # make it visible to dotnet via csproj
Write-Host "STS2 game dir: $GameDir"

# ---------------------------------------------------------------------------
# Resolve STS1 unpacked dir (only when we're building assets)
# ---------------------------------------------------------------------------
if (-not $SkipAssets) {
  if (-not $Sts1Dir) { $Sts1Dir = $env:STS1_UNPACKED_DIR }
  if (-not $Sts1Dir) {
    # Legacy fallback: sibling folder next to the mod source, same name
    # layout the in-repo build used.
    $legacy = Join-Path (Split-Path -Parent (Split-Path -Parent $projectDir)) "SlayTheSpire_unpacked"
    if (Test-Path $legacy) { $Sts1Dir = $legacy }
  }
  if (-not $Sts1Dir -or -not (Test-Path $Sts1Dir)) {
    throw @"
ClassicMode needs an unpacked Slay the Spire 1 assets directory to build its
portraits and localization. Provide it with one of:
    -Sts1Dir "C:\path\to\SlayTheSpire_unpacked"
    `$env:STS1_UNPACKED_DIR = "C:\path\to\SlayTheSpire_unpacked"
    -SkipAssets                (to skip asset preparation entirely)
See README.md for how to unpack Slay the Spire 1.
"@
  }
  $Sts1Dir = (Resolve-Path $Sts1Dir).Path
  $env:STS1_UNPACKED_DIR = $Sts1Dir
  Write-Host "STS1 unpacked dir: $Sts1Dir"
}

# ---------------------------------------------------------------------------
# Dot-source shared build helpers
# ---------------------------------------------------------------------------
. (Join-Path $projectDir "scripts\mod_build_common.ps1")

# ---------------------------------------------------------------------------
# Layout
# ---------------------------------------------------------------------------
$project = Join-Path $projectDir "$modName.csproj"
$outputRoot = Join-Path $projectDir "build"
$outputDir = Join-Path $outputRoot $modName
$buildDir = Join-Path $projectDir "bin\Release\net9.0"
$pckRoot = Join-Path $outputDir "_pck_src"
$pckPath = Join-Path $outputDir "$modName.pck"

Ensure-Dir $outputRoot
Ensure-Dir $outputDir
if ($SkipAssets) {
  if (-not (Test-Path $pckRoot)) {
    throw @"
Cannot use -SkipAssets because no existing PCK source directory was found:
  $pckRoot

Run build.ps1 once without -SkipAssets to generate _pck_src, then use -SkipAssets for faster iteration.
"@
  }
  Write-Host "Reusing existing PCK source: $pckRoot"
}
else {
  if (Test-Path $pckRoot) {
    Remove-Item $pckRoot -Recurse -Force
  }
  Ensure-Dir $pckRoot
}

# ---------------------------------------------------------------------------
# Step 1: Build the DLL
# ---------------------------------------------------------------------------
Write-Host "Building $modName..."
dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) {
  throw "dotnet build failed with exit code $LASTEXITCODE"
}

Copy-Item (Join-Path $buildDir "*") $outputDir -Recurse -Force

# Manifest: <id>.json next to the DLL (0.99+) + inside PCK (0.98.x compat)
Copy-Item (Join-Path $projectDir "mod_manifest.json") (Join-Path $outputDir "$modName.json") -Force
Copy-Item (Join-Path $projectDir "mod_manifest.json") (Join-Path $pckRoot "mod_manifest.json") -Force
Copy-IfExists (Join-Path $projectDir "README.md") (Join-Path $outputDir "README.md")

# ---------------------------------------------------------------------------
# Step 2: Prepare assets (STS1 portraits, relics, localization)
# ---------------------------------------------------------------------------
if ($SkipAssets) {
  Write-Host "Skipping prepare_assets.py (as requested by -SkipAssets)"
}
else {
  Write-Host "Preparing assets..."
  python (Join-Path $projectDir "prepare_assets.py") $projectDir $pckRoot $Sts1Dir
  if ($LASTEXITCODE -ne 0) {
    throw "prepare_assets.py failed with exit code $LASTEXITCODE"
  }
}

# ---------------------------------------------------------------------------
# Step 3: Godot texture import (skips if no images or no Godot)
# ---------------------------------------------------------------------------
Import-GodotTextures -PckRoot $pckRoot -ModName $modName

# ---------------------------------------------------------------------------
# Step 4: Pack PCK + ZIPs
# ---------------------------------------------------------------------------
# Validate staged PCK source before packing to avoid generating an empty/stub PCK.
$pckSourceFiles = Get-ChildItem $pckRoot -Recurse -File
$pckSourceCount = @($pckSourceFiles).Count
$pckSourceBytes = ($pckSourceFiles | Measure-Object -Property Length -Sum).Sum
if (-not $pckSourceBytes) { $pckSourceBytes = 0 }

if ($pckSourceCount -lt 20 -or $pckSourceBytes -lt 5MB) {
  throw @"
PCK source looks incomplete and is likely to produce a bad package:
  Root:  $pckRoot
  Files: $pckSourceCount
  Size:  $pckSourceBytes bytes

Re-run build.ps1 without -SkipAssets to regenerate _pck_src from STS1 assets.
"@
}

Build-ModPck   -PckRoot $pckRoot -PckPath $pckPath

# Guard against accidentally packaging an empty/stub PCK (e.g. 608 bytes).
if (-not (Test-Path $pckPath)) {
  throw "PCK was not generated at: $pckPath"
}
$pckSize = (Get-Item $pckPath).Length
if ($pckSize -lt 1MB) {
  throw @"
Generated PCK is suspiciously small ($pckSize bytes):
  $pckPath

This usually means assets were not packaged. Re-run without -SkipAssets and verify STS1_UNPACKED_DIR.
"@
}

Package-ModZips -OutputDir $outputDir -ModName $modName -PckPath $pckPath -RepoRoot $outputRoot -ProjectDir $projectDir
