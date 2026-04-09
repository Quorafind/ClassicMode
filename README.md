# Classic Mode — Slay the Spire 2 mod

Play Slay the Spire 2 characters with **Slay the Spire 1** card pools, relic
pools, and boss content. Classic Cards, Classic Relics, and Classic Bosses
can be toggled independently on the character-select screen.

> 中文说明请见 [README.zh-CN.md](README.zh-CN.md)

## Requirements

To build this mod you need:

1. **Slay the Spire 2** — the game DLLs this mod compiles against live in
   `<STS2 install>\data_sts2_windows_x86_64\`.
2. **.NET 9 SDK** — `dotnet --version` should report `9.x`.
3. **Python 3.9+** with `Pillow` installed (`pip install pillow`).
4. **Godot 4.5+ (Mono build)** — only required if you want the mod to run
   on Android. On desktop the build script will skip the Godot import step
   gracefully if the editor isn't found.
5. **An unpacked copy of Slay the Spire 1's assets.** See next section.

### Unpacking Slay the Spire 1 assets

ClassicMode reuses STS1 card portraits, relic icons, and English/Chinese
localization. It does **not** ship any STS1 assets in this repo — you must
bring your own unpacked STS1 directory.

You can produce one yourself from a legitimate copy of STS1:

1. Grab a JAR-unpacking tool (e.g. [jd-cli](https://github.com/intoolswetrust/jd-cli))
   or simply unzip `SlayTheSpire.jar` from your Steam install.
2. Extract the contents so you end up with a directory tree that looks
   roughly like:
   ```
   SlayTheSpire_unpacked/
     images/
       1024Portraits/red/attack/*.png
       1024Portraits/green/...
       1024Portraits/blue/...
       relics/*.png
     localization/
       eng/cards.json
       eng/relics.json
       zhs/cards.json
       zhs/relics.json
   ```
3. Point the build at it via `-Sts1Dir` or the `STS1_UNPACKED_DIR`
   environment variable (see below).

Without this directory the build will fail fast with instructions. If you
only want to iterate on C# code you can run with `-SkipAssets` and reuse a
previously-built `_pck_src`.

## Building

From the repo root, in PowerShell:

```powershell
# Option A: environment variables (recommended for repeat builds)
$env:STS2_GAME_DIR     = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
$env:STS1_UNPACKED_DIR = "C:\path\to\SlayTheSpire_unpacked"
.\build.ps1

# Option B: command-line parameters
.\build.ps1 `
  -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2" `
  -Sts1Dir "C:\path\to\SlayTheSpire_unpacked"

# Option C: skip asset prep (C#-only iteration)
.\build.ps1 -SkipAssets
```

`build.ps1` tries to auto-detect a few common Steam install paths, so on a
typical setup the first run may "just work" with only `-Sts1Dir` specified.

## Build output

A successful build drops everything under `build/`:

```
build/
  ClassicMode/
    ClassicMode.dll
    ClassicMode.json        # 0.99+ manifest
    ClassicMode.pck         # packed assets
  ClassicMode-STS2_0.98.x-<version>.zip
  ClassicMode-STS2_0.99-<version>.zip
```

Install by dropping the whole `ClassicMode/` folder into the game's `mods/`
directory, or by distributing one of the zips.

## Repository layout

```
ClassicMode/
├── Base/                 # Base card/relic classes
├── Cards/                # Ironclad / Silent / Defect card definitions
├── Encounters/           # Classic bosses (Hexaghost, etc.)
├── Monsters/             # Classic monster models
├── Patches/              # Harmony patches
├── Pools/                # Card / relic pool definitions
├── Powers/               # Classic power models
├── Relics/               # Classic relic definitions
├── assets/               # Localization + manually-shipped images
├── scripts/              # Shared build helpers (see below)
│   ├── mod_build_common.ps1
│   ├── pack_godot_pck.py
│   └── import_assets.py
├── ClassicBootstrap.cs   # Mod entry point + Harmony bootstrapper
├── ClassicConfig.cs      # Persisted user toggles
├── ClassicMode.csproj    # Project file (reads STS2_GAME_DIR)
├── prepare_assets.py     # Turns STS1 unpacked assets into _pck_src content
├── build.ps1             # Standalone build entry point
├── mod_manifest.json
├── LICENSE               # MIT
└── README.md
```

### `scripts/` directory

`scripts/` contains the PowerShell / Python helpers the build needs to turn
raw source into a packed `.pck`:

- **`mod_build_common.ps1`** — reusable PowerShell functions: locating
  Godot, running headless texture import, packing the PCK, and zipping
  release archives.
- **`pack_godot_pck.py`** — minimal pure-Python Godot 4 PCK packer.
- **`import_assets.py`** — runs after Godot's headless import to relocate
  `.ctex` files out of `.godot/imported/` (so the packed PCK doesn't
  collide with the host game's own `.godot/`).

None of these scripts depend on anything outside this repository — they are
self-contained copies that can be edited in place.

## License

[MIT](LICENSE).

ClassicMode ships no assets from either Slay the Spire game; it only
references STS2 DLLs at build time and pulls STS1 assets from the user's
local unpacked copy at asset-prep time.
