"""
Prepare ClassicMode mod assets:
  - Generate STS2-format localization from STS1 data
  - Copy STS1 card portraits (red/green/blue) into PCK source
  - Copy STS1 relic images
  - Generate .import + .ctex metadata for Godot ResourceLoader

Usage:
  python prepare_assets.py <project_dir> <pck_root> [<sts1_unpacked_dir>]

If <sts1_unpacked_dir> is omitted, the script looks for it in the
STS1_UNPACKED_DIR environment variable. It must point at an unpacked
Slay the Spire 1 assets tree (containing images/1024Portraits/,
images/relics/, localization/eng/cards.json, etc.).
"""

import glob
import json
import os
import re
import shutil
import struct
import sys

# ---------------------------------------------------------------------------
# Godot .ctex / .import generation (same approach as Watcher's prepare_assets.py)
# ---------------------------------------------------------------------------

def write_webp_ctex(src_png: str, dst_ctex: str):
    """Convert a PNG to Godot CompressedTexture2D (.ctex) using WebP lossless."""
    try:
        from PIL import Image
        import io
        img = Image.open(src_png)
        w, h = img.size
        buf = io.BytesIO()
        img.save(buf, format="WEBP", lossless=True)
        webp_data = buf.getvalue()
    except ImportError:
        # Pillow not available — skip .ctex generation
        return False

    # CompressedTexture2D binary format (Godot 4.x)
    with open(dst_ctex, "wb") as f:
        f.write(b"GST2")                 # magic
        f.write(struct.pack("<I", 1))     # version
        f.write(struct.pack("<I", w))     # width
        f.write(struct.pack("<I", h))     # height
        f.write(struct.pack("<I", 0))     # flags
        f.write(struct.pack("<I", 0))     # padding
        # image block
        f.write(struct.pack("<I", w))
        f.write(struct.pack("<I", h))
        f.write(struct.pack("<I", 0))     # mipmaps
        f.write(struct.pack("<I", 38))    # format = WebP
        # data size + data
        f.write(struct.pack("<I", len(webp_data)))
        f.write(webp_data)

    return True


def write_import_file(res_path: str, import_path: str, dest_dir: str):
    """Write a .import metadata file so Godot's ResourceLoader discovers the texture."""
    import_file = os.path.join(dest_dir, os.path.basename(res_path) + ".import")
    content = f"""[remap]

importer="texture"
type="CompressedTexture2D"
uid=""
path="{import_path}"

[deps]

source_file="{res_path}"
dest_files=["{import_path}"]
"""
    with open(import_file, "w", encoding="utf-8") as f:
        f.write(content)


# ---------------------------------------------------------------------------
# Portrait name mapping from STS1 class-style names
# ---------------------------------------------------------------------------

# STS1 relic images that are character-specific starters
STARTER_RELICS = {
    "ironclad": ["burningBlood"],
    "silent": ["bag_of_prep"],  # Ring of the Snake image name
    "defect": ["cables"],       # Cracked Core image name
}

# Character-specific relic images (approximate — will be refined in Phase 5)
CHARACTER_RELICS = {
    "ironclad": [
        "burningBlood", "blackBlood",  # starter + upgrade
        "burnHammer", "brimstone", "selfForming_clay",
        "champion_belt", "paper_krane", "mark_of_pain",
    ],
    "silent": [
        "bag_of_prep", "holyWater",  # Ring of Snake + upgrade
        "toxicEgg", "dvu_energy", "wristBlade",
        "tinyCoffin", "tingsha", "toolbox",
    ],
    "defect": [
        "cables", "nuclear",  # Cracked Core + upgrade
        "data_disk", "symbiotic_virus", "emotion_chip",
        "runic_capacitor", "inserter", "gold_plated_cables",
    ],
}


_CARD_BASE_CLASS_TO_COLORS = {
    "ClassicIroncladCard": ("red", "ironclad"),
    "ClassicSilentCard": ("green", "silent"),
    "ClassicDefectCard": ("blue", "defect"),
    "ClassicColorlessCard": ("colorless", "colorless"),
}

_CARD_TYPE_TO_PORTRAIT_SUBDIR = {
    "Attack": "attack",
    "Skill": "skill",
    "Power": "power",
}

_PORTRAIT_SOURCE_BASENAME_OVERRIDES = {
    ("defect", "compiled_driver"): "compile_driver",
}

# STS1 power icon import map:
# - small: atlas entry used for under-character power row icon (generated as Godot AtlasTexture .tres)
# - big: atlas entry cropped to powers/<id>.png for large tooltip panel fallback
_STS1_POWER_ICON_IMPORT_MAP = {
    "FLEX_POWER": {"small": "48/flex", "big": "128/flex"},
    "BERSERK_POWER_C": {"small": "48/berserk", "big": "128/berserk"},
    "COMBUST_POWER_C": {"small": "48/combust", "big": "128/combust"},
    "EVOLVE_POWER_C": {"small": "48/evolve", "big": "128/evolve"},
    "FIRE_BREATHING_POWER_C": {"small": "48/firebreathing", "big": "128/firebreathing"},
    "METALLICIZE_POWER_C": {"small": "48/armor", "big": "128/armor"},
    "BRUTALITY_POWER_C": {"small": "48/brutality", "big": "128/brutality"},
    "A_THOUSAND_CUTS_POWER": {"small": "48/thousandCuts", "big": "128/thousandCuts"},
    "CHOKE_HOLD_POWER": {"small": "48/choke", "big": "128/choke"},
    "CORPSE_EXPLOSION_POWER": {"small": "48/cExplosion", "big": "128/cExplosion"},
    "PHANTASMAL_KILLER_POWER": {"small": "48/phantasmal", "big": "128/phantasmal"},
    "ELECTRODYNAMICS_POWER_C": {"small": "48/channel", "big": "128/channel"},
    "STATIC_DISCHARGE_POWER_C": {"small": "48/static_discharge", "big": "128/static_discharge"},
    "CREATIVE_AI_POWER_C": {"small": "48/ai", "big": "128/ai"},
    "HELLO_WORLD_POWER_C": {"small": "48/hello", "big": "128/hello"},
    "STORM_POWER_C": {"small": "48/storm", "big": "128/storm"},
    "MACHINE_LEARNING_POWER_C": {"small": "48/carddraw", "big": "128/carddraw"},
    "SELF_REPAIR_POWER_C": {"small": "48/repair", "big": "128/repair"},
    "LOOP_POWER_C": {"small": "48/loop", "big": "128/loop"},
    "HEATSINKS_POWER_C": {"small": "48/heatsink", "big": "128/heatsink"},
    "LOCK_ON_POWER_C": {"small": "48/lockon", "big": "128/lockon"},
    "REBOUND_POWER_C": {"small": "48/rebound", "big": "128/rebound"},
    "AMPLIFY_POWER_C": {"small": "48/amplify", "big": "128/amplify"},
    "ECHO_FORM_POWER_C": {"small": "48/echo", "big": "128/echo"},
    "RUPTURE_POWER_C": {"small": "48/rupture", "big": "128/rupture"},
}

_PLACEHOLDER_CASE_MAP = {
    "amount": "Amount",
    "magicnumber": "MagicNumber",
}


def _normalize_portrait_basename(name: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", name.lower())


def _build_portrait_source_index(base_dir: str):
    index = {}
    for color_dir in ("red", "green", "blue", "colorless"):
        color_index = {}
        for sub_dir in ("attack", "skill", "power"):
            path = os.path.join(base_dir, color_dir, sub_dir)
            exact = {}
            normalized = {}
            if os.path.isdir(path):
                for fname in os.listdir(path):
                    if not fname.lower().endswith(".png"):
                        continue
                    basename = os.path.splitext(fname)[0]
                    file_path = os.path.join(path, fname)
                    exact[basename] = file_path
                    normalized.setdefault(_normalize_portrait_basename(basename), []).append(
                        (basename, file_path)
                    )
            color_index[sub_dir] = {"exact": exact, "normalized": normalized}
        index[color_dir] = color_index
    return index


def _find_card_portrait_specs(project_dir: str):
    """Scan ClassicMode card classes for portrait basenames and card types."""
    specs = []
    cards_dir = os.path.join(project_dir, "Cards")
    class_re = re.compile(r'public sealed class (\w+)\s*:\s*(Classic\w*Card)')

    for cs_file in glob.glob(os.path.join(cards_dir, "**", "*.cs"), recursive=True):
        with open(cs_file, 'r', encoding='utf-8') as f:
            content = f.read()

        matches = list(class_re.finditer(content))
        for idx, match in enumerate(matches):
            class_name, base_class = match.groups()
            if base_class not in _CARD_BASE_CLASS_TO_COLORS:
                continue

            class_end = matches[idx + 1].start() if idx + 1 < len(matches) else len(content)
            class_body = content[match.start():class_end]
            ctor_match = re.search(
                rf'public\s+{re.escape(class_name)}\s*\(\)\s*.*?:\s*base\("([^"]+)",\s*[^,]+,\s*CardType\.(Attack|Skill|Power)',
                class_body,
                flags=re.S,
            )
            if not ctor_match:
                print(f"  WARNING: Could not parse portrait metadata for {class_name}")
                continue

            portrait_name, card_type = ctor_match.groups()
            source_color, char_name = _CARD_BASE_CLASS_TO_COLORS[base_class]
            specs.append(
                {
                    "class_name": class_name,
                    "char_name": char_name,
                    "source_color": source_color,
                    "portrait_name": portrait_name,
                    "card_type": card_type,
                }
            )

    deduped = {}
    for spec in specs:
        key = (spec["char_name"], spec["portrait_name"])
        existing = deduped.get(key)
        if existing and existing["card_type"] != spec["card_type"]:
            raise ValueError(
                f"Portrait basename collision in card defs for {spec['char_name']}/{spec['portrait_name']}: "
                f"{existing['class_name']} is {existing['card_type']}, {spec['class_name']} is {spec['card_type']}"
            )
        deduped.setdefault(key, spec)

    return sorted(deduped.values(), key=lambda spec: (spec["char_name"], spec["portrait_name"]))


def _resolve_portrait_source(source_index, spec):
    source_subdir = _CARD_TYPE_TO_PORTRAIT_SUBDIR[spec["card_type"]]
    folder_index = source_index.get(spec["source_color"], {}).get(source_subdir, {})
    exact = folder_index.get("exact", {})
    normalized = folder_index.get("normalized", {})

    desired_name = spec["portrait_name"]
    override_name = _PORTRAIT_SOURCE_BASENAME_OVERRIDES.get((spec["char_name"], desired_name))

    for candidate in [name for name in (override_name, desired_name) if name]:
        path = exact.get(candidate)
        if path:
            return path, candidate

    for candidate in [name for name in (override_name, desired_name) if name]:
        matches = normalized.get(_normalize_portrait_basename(candidate), [])
        if len(matches) == 1:
            matched_name, path = matches[0]
            return path, matched_name
        if len(matches) > 1:
            print(
                f"  WARNING: Ambiguous normalized portrait match for "
                f"{spec['class_name']} ({candidate}) in {spec['source_color']}/{source_subdir}"
            )
            return None, None

    return None, None


def copy_card_portraits(sts1_root: str, project_dir: str, pck_root: str):
    """
    Copy the card portraits referenced by ClassicMode cards into the PCK source.

    Output filenames come from the portrait basenames declared in card constructors.
    Source selection is deterministic: the STS1 portrait subfolder is chosen from
    the card's CardType, so same-name files across attack/skill/power never rely
    on copy order.
    """
    portraits_base = os.path.join(sts1_root, "images", "1024Portraits")
    beta_base = os.path.join(sts1_root, "images", "1024PortraitsBeta")
    normal_index = _build_portrait_source_index(portraits_base)
    beta_index = _build_portrait_source_index(beta_base)
    card_specs = _find_card_portrait_specs(project_dir)

    total_copied = 0
    alias_count = 0
    beta_missing = 0
    normal_missing = []

    for spec in card_specs:
        normal_src, resolved_name = _resolve_portrait_source(normal_index, spec)
        if not normal_src:
            normal_missing.append(spec)
            continue

        dst_dir = os.path.join(
            pck_root, "images", "packed", "card_portraits", "classic", spec["char_name"]
        )
        os.makedirs(dst_dir, exist_ok=True)
        shutil.copy2(normal_src, os.path.join(dst_dir, spec["portrait_name"] + ".png"))
        total_copied += 1
        if resolved_name != spec["portrait_name"]:
            alias_count += 1

        beta_src, beta_resolved_name = _resolve_portrait_source(beta_index, spec)
        if beta_src:
            beta_dst = os.path.join(dst_dir, "beta")
            os.makedirs(beta_dst, exist_ok=True)
            shutil.copy2(beta_src, os.path.join(beta_dst, spec["portrait_name"] + ".png"))
            total_copied += 1
            if beta_resolved_name != spec["portrait_name"]:
                alias_count += 1
        else:
            beta_missing += 1

    if normal_missing:
        missing_list = ", ".join(
            f"{spec['class_name']} ({spec['char_name']}/{spec['card_type']}/{spec['portrait_name']})"
            for spec in normal_missing[:10]
        )
        extra = "" if len(normal_missing) <= 10 else f" ... +{len(normal_missing) - 10} more"
        print(f"  WARNING: Missing {len(normal_missing)} required base portrait(s): {missing_list}{extra}")

    if beta_missing:
        print(f"  NOTE: Missing {beta_missing} beta portrait(s) in STS1 source.")
    if alias_count:
        print(f"  Applied {alias_count} portrait filename alias/normalization resolution(s).")
    print(f"  Copied {total_copied} card portraits.")
    return total_copied


def copy_relic_images(sts1_root: str, pck_root: str):
    """Copy STS1 relic images into the PCK source."""
    relics_src = os.path.join(sts1_root, "images", "relics")
    if not os.path.isdir(relics_src):
        print(f"  WARNING: Missing relic images directory: {relics_src}")
        return 0

    relics_dst = os.path.join(pck_root, "images", "relics", "classic")
    os.makedirs(relics_dst, exist_ok=True)
    outline_dst = os.path.join(relics_dst, "outline")
    os.makedirs(outline_dst, exist_ok=True)

    pil_image = None
    try:
        from PIL import Image as _PILImage
        pil_image = _PILImage
    except ImportError:
        print("  NOTE: Pillow not installed - relic icon crop disabled; copying original size.")

    def _copy_relic_png(src_path: str, dst_path: str):
        if pil_image is None:
            shutil.copy2(src_path, dst_path)
            return

        with pil_image.open(src_path) as img:
            w, h = img.size
            if w <= 1 or h <= 1:
                img.save(dst_path)
                return

            # Crop 1/4 on each side: keep center 1/2 width and 1/2 height.
            crop_w = max(1, w // 2)
            crop_h = max(1, h // 2)
            left = (w - crop_w) // 2
            top = (h - crop_h) // 2
            right = left + crop_w
            bottom = top + crop_h
            cropped = img.crop((left, top, right, bottom))
            cropped.save(dst_path)

    total = 0
    for fname in os.listdir(relics_src):
        if not fname.lower().endswith(".png"):
            continue
        _copy_relic_png(os.path.join(relics_src, fname), os.path.join(relics_dst, fname))
        total += 1

    # Also copy outline versions if they exist
    outline_src = os.path.join(relics_src, "outline")
    if os.path.isdir(outline_src):
        for fname in os.listdir(outline_src):
            if not fname.lower().endswith(".png"):
                continue
            _copy_relic_png(os.path.join(outline_src, fname), os.path.join(outline_dst, fname))

    # Compatibility aliases for code-side camelCase asset keys.
    # Some STS1 relic files are snake_case; keep both names in packed assets.
    alias_pairs = [
        ("ink_bottle.png", "inkBottle.png"),
        ("serpent_ring.png", "ringOfTheSerpent.png"),
        ("frozenOrb.png", "frozenCore.png"),
        ("mark_of_pain.png", "markOfPain.png"),
        ("crown.png", "bustedCrown.png"),
        ("burnHammer.png", "fusionHammer.png"),
        ("bark.png", "sacredBark.png"),
        ("collar.png", "slaversCollar.png"),
    ]
    for src_name, alias_name in alias_pairs:
        src_main = os.path.join(relics_dst, src_name)
        dst_main = os.path.join(relics_dst, alias_name)
        if os.path.isfile(src_main) and not os.path.isfile(dst_main):
            shutil.copy2(src_main, dst_main)

        src_outline = os.path.join(outline_dst, src_name)
        dst_outline = os.path.join(outline_dst, alias_name)
        if os.path.isfile(src_outline) and not os.path.isfile(dst_outline):
            shutil.copy2(src_outline, dst_outline)

    print(f"  Copied {total} relic images.")
    return total


def copy_custom_enchantment_icons(pck_root: str):
    """Create custom enchantment icons from already-cropped relic art."""
    relic_icon = os.path.join(pck_root, "images", "relics", "classic", "bottledLightning.png")
    enchantments_dir = os.path.join(pck_root, "images", "enchantments")
    os.makedirs(enchantments_dir, exist_ok=True)

    if not os.path.isfile(relic_icon):
        print(f"  WARNING: Source relic icon not found for BottledEnchantment: {relic_icon}")
        return 0

    dst_icon = os.path.join(enchantments_dir, "bottled_enchantment.png")
    shutil.copy2(relic_icon, dst_icon)
    print(f"  Copied custom enchantment icon: {dst_icon}")
    return 1


def _parse_sts1_atlas_entries(atlas_path: str):
    """Parse a LibGDX atlas file and return entry -> {xy, size}."""
    entries = {}
    current_name = None
    current_data = {}

    with open(atlas_path, "r", encoding="utf-8") as f:
        for raw in f:
            line = raw.rstrip("\n")
            stripped = line.strip()
            if not stripped:
                continue

            # Atlas entry header (non-indented, not a key:value metadata line)
            if not line.startswith(" ") and ":" not in stripped:
                if current_name and "xy" in current_data and "size" in current_data:
                    entries[current_name] = current_data
                current_name = stripped
                current_data = {}
                continue

            if current_name is None:
                continue

            if stripped.startswith("xy:"):
                x_str, y_str = stripped.split(":", 1)[1].split(",")
                current_data["xy"] = (int(x_str.strip()), int(y_str.strip()))
            elif stripped.startswith("size:"):
                w_str, h_str = stripped.split(":", 1)[1].split(",")
                current_data["size"] = (int(w_str.strip()), int(h_str.strip()))

    if current_name and "xy" in current_data and "size" in current_data:
        entries[current_name] = current_data

    return entries


def _write_atlas_texture_tres(tres_path: str, atlas_res_path: str, xy: tuple[int, int], size: tuple[int, int]):
    x, y = xy
    w, h = size
    content = (
        '[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n'
        f'[ext_resource type="Texture2D" path="{atlas_res_path}" id="1"]\n\n'
        '[resource]\n'
        'atlas = ExtResource("1")\n'
        f'region = Rect2({x}, {y}, {w}, {h})\n'
    )
    with open(tres_path, "w", encoding="utf-8") as f:
        f.write(content)


def copy_power_icons_from_sts1(sts1_root: str, pck_root: str):
    """
    Import selected STS1 power icons into STS2 paths expected by PowerModel.

    This creates:
    - images/atlases/power_atlas.sprites/<power_id_lower>.tres  (atlas-loader compatible path)
    - images/powers/<power_id_lower>.tres                       (direct resource path, bypasses atlas lookup)
    - images/powers/<power_id_lower>.png                        (optional big icon crop)
    """
    src_png = os.path.join(sts1_root, "powers", "powers.png")
    src_atlas = os.path.join(sts1_root, "powers", "powers.atlas")
    if not os.path.isfile(src_png) or not os.path.isfile(src_atlas):
        print(f"  WARNING: Missing STS1 power atlas assets: {src_png} / {src_atlas}")
        return 0

    entries = _parse_sts1_atlas_entries(src_atlas)

    images_dst = os.path.join(pck_root, "images")
    os.makedirs(images_dst, exist_ok=True)

    atlases_dst = os.path.join(images_dst, "atlases")
    sprites_dst = os.path.join(atlases_dst, "power_atlas.sprites")
    os.makedirs(sprites_dst, exist_ok=True)
    powers_dst = os.path.join(images_dst, "powers")
    os.makedirs(powers_dst, exist_ok=True)

    atlas_png_name = "powers.png"
    atlas_png_dst = os.path.join(images_dst, atlas_png_name)
    shutil.copy2(src_png, atlas_png_dst)

    imported_small = 0
    imported_big_tres = 0
    imported_big_png = 0
    can_extract_big = True
    image_mod = None

    try:
        from PIL import Image as _PILImage
        image_mod = _PILImage
    except ImportError:
        # Big icon extraction is optional; keep importing all small atlas icons.
        can_extract_big = False
        print("  NOTE: Pillow not installed — skipping big power icon extraction.")

    for model_id, cfg in _STS1_POWER_ICON_IMPORT_MAP.items():
        model_slug = model_id.lower()

        small_key = cfg.get("small")
        small_entry = entries.get(small_key) if small_key else None
        if not small_entry:
            print(f"  WARNING: Missing STS1 small power entry for {model_id}: {small_key}")
            continue

        tres_path = os.path.join(sprites_dst, f"{model_slug}.tres")
        _write_atlas_texture_tres(
            tres_path=tres_path,
            atlas_res_path=f"res://images/{atlas_png_name}",
            xy=small_entry["xy"],
            size=small_entry["size"],
        )

        # Direct-load fallback path that does not go through AtlasResourceLoader parsing.
        _write_atlas_texture_tres(
            tres_path=os.path.join(powers_dst, f"{model_slug}.tres"),
            atlas_res_path=f"res://images/{atlas_png_name}",
            xy=small_entry["xy"],
            size=small_entry["size"],
        )
        imported_small += 1

        big_key = cfg.get("big")
        big_entry = entries.get(big_key) if big_key else None
        if not big_entry:
            continue

        # Always emit a big AtlasTexture resource so runtime big icon loading
        # does not depend on Pillow being installed.
        _write_atlas_texture_tres(
            tres_path=os.path.join(powers_dst, f"{model_slug}_big.tres"),
            atlas_res_path=f"res://images/{atlas_png_name}",
            xy=big_entry["xy"],
            size=big_entry["size"],
        )
        imported_big_tres += 1

        if not can_extract_big or image_mod is None:
            continue

        with image_mod.open(src_png) as image:
            x, y = big_entry["xy"]
            w, h = big_entry["size"]
            cropped = image.crop((x, y, x + w, y + h))
            cropped.save(os.path.join(powers_dst, f"{model_slug}.png"))
            imported_big_png += 1

    print(
        f"  Imported {imported_small} STS1 small power icon(s), "
        f"{imported_big_tres} big .tres icon(s), and {imported_big_png} big .png icon(s)."
    )
    return imported_small


def copy_localization(project_dir: str, pck_root: str):
    """Copy localization JSON files from assets/ to PCK root."""
    assets_dir = os.path.join(project_dir, "assets")
    if not os.path.isdir(assets_dir):
        return

    for root, dirs, files in os.walk(assets_dir):
        for fname in files:
            src = os.path.join(root, fname)
            rel = os.path.relpath(src, assets_dir)
            dst = os.path.join(pck_root, rel)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copy2(src, dst)


def generate_ctex_for_images(pck_root: str):
    """
    Generate .import + .ctex files for all PNGs in the PCK source.
    This enables Godot's ResourceLoader to find mod textures.
    """
    try:
        from PIL import Image
    except ImportError:
        print("  Pillow not installed — skipping .ctex generation (raw PNGs will be packed as-is)")
        return 0

    total = 0
    for root, dirs, files in os.walk(pck_root):
        for fname in files:
            if not fname.lower().endswith(".png"):
                continue

            src_path = os.path.join(root, fname)
            rel_path = os.path.relpath(src_path, pck_root).replace("\\", "/")
            res_path = f"res://{rel_path}"

            # .ctex goes alongside the .png
            ctex_name = fname + ".ctex"
            ctex_path = os.path.join(root, ctex_name)

            # .import file for the .ctex
            import_res = f"res://ClassicMode/_imported/{rel_path}.ctex"

            if write_webp_ctex(src_path, ctex_path):
                write_import_file(res_path, import_res, root)
                total += 1

    print(f"  Generated {total} .ctex textures.")
    return total


# ---------------------------------------------------------------------------
# Localization generation from STS1 data
# ---------------------------------------------------------------------------

# ClassicMode card class → STS1 card ID (only non-obvious mappings)
# STS1 IDs are the JSON keys in localization/eng/cards.json
_CARD_OVERRIDES = {
    # Character starters have _R/_G/_B suffixes in STS1
    "StrikeIronclad_C": "Strike_R",
    "DefendIronclad_C": "Defend_R",
    "StrikeSilent_C": "Strike_G",
    "DefendSilent_C": "Defend_G",
    "StrikeDefect_C": "Strike_B",
    "DefendDefect_C": "Defend_B",
    # STS1 internal IDs differ from display names
    "Claw_C": "Gash",                       # STS1 internal "Gash" displays as "Claw"
    "Recursion_C": "Redo",                   # STS1 internal "Redo" displays as "Recursion"
    "Alchemize_C": "Venomology",             # STS1 internal "Venomology" displays as "Alchemize"
    "Equilibrium_C": "Undo",                 # STS1 internal "Undo" displays as "Equilibrium"
    "SneakyStrike_C": "Underhanded Strike",  # STS1 internal "Underhanded Strike" displays as "Sneaky Strike"
    "WraithForm_C": "Wraith Form v2",
    "ChargeBattery_C": "Conserve Battery",   # STS1 internal displays as "Charge Battery"
    "CompiledDriver_C": "Compile Driver",
    "ChokeHold_C": "Choke",
    "SteamBarrier_C": "Steam",               # STS1 internal "Steam" displays as "Steam Barrier"
    "Lockdown_C": "Lockon",                  # STS1 internal "Lockon" displays as "Bullseye"/"Lock-On"
    "Nightmare_C": "Night Terror",           # STS1 internal "Night Terror" displays as "Nightmare"
    "Overclock_C": "Steam Power",            # STS1 internal "Steam Power" displays as "Overclock"
}

# ClassicMode custom relic class → STS1 relic ID
_RELIC_MAP = {
    "MarkOfPain": "Mark of Pain",
    "ChampionBelt": "Champion Belt",
    "WristBlade": "WristBlade",
    "HoveringKite": "HoveringKite",
    "TheSpecimen": "The Specimen",
    "FrozenCore": "FrozenCore",
    "Inserter": "Inserter",
    "NuclearBattery": "Nuclear Battery",
}

# ClassicMode custom power class → STS1 power ID
_POWER_MAP = {
    "CombustPower_C": "Combust",
    "EvolvePower_C": "Evolve",
    "FireBreathingPower_C": "Fire Breathing",
    "MetallicizePower_C": "Metallicize",
    "BerserkPower_C": "Berserk",
    "BrutalityPower_C": "Brutality",
    "AThousandCutsPower": "Thousand Cuts",
    "ChokeHoldPower": "Choked",
    "CorpseExplosionPower": "CorpseExplosionPower",
    "PhantasmalKillerPower": "Phantasmal",
    "ElectrodynamicsPower_C": "Electro",
    "StaticDischargePower_C": "StaticDischarge",
    "CreativeAiPower_C": "Creative AI",
    "HelloWorldPower_C": "Hello",
    "StormPower_C": "Storm",
    "MachineLearningPower_C": "Machine Learning",
    "SelfRepairPower_C": "Repair",
    "LoopPower_C": "Loop",
    "HeatsinksPower_C": "Heatsink",
    "EchoFormPower_C": "Echo Form",
    "SadisticNaturePower_C": "Sadistic",
    "MagnetismPower_C": "Magnetism",
}

# Hardcoded power descriptions (auto-generation from STS1 arrays is unreliable)
_CUSTOM_POWER_LOC = {
    "eng": {
        "FlexPower": {"NAME": "Strength Down", "DESCRIPTION": "Gain {Amount} Strength. At the end of your turn, lose {Amount} Strength."},
        "CombustPower_C": {"NAME": "Combust", "DESCRIPTION": "At the end of your turn, lose 1 HP and deal {Amount} damage to ALL enemies."},
        "EvolvePower_C": {"NAME": "Evolve", "DESCRIPTION": "Whenever you draw a Status card, draw {Amount} cards."},
        "FireBreathingPower_C": {"NAME": "Fire Breathing", "DESCRIPTION": "Whenever you draw a Status or Curse card, deal {Amount} damage to ALL enemies."},
        "MetallicizePower_C": {"NAME": "Metallicize", "DESCRIPTION": "At the end of your turn, gain {Amount} Block."},
        "BerserkPower_C": {"NAME": "Berserk", "DESCRIPTION": "At the start of your turn, gain {energyPrefix:energyIcons(1)}."},
        "BrutalityPower_C": {"NAME": "Brutality", "DESCRIPTION": "At the start of your turn, lose 1 HP and draw {Amount} cards."},
        "RupturePower_C": {"NAME": "Rupture", "DESCRIPTION": "Whenever you lose HP from a card, gain {Amount} [gold]Strength[/gold]."},
        "AThousandCutsPower": {"NAME": "Thousand Cuts", "DESCRIPTION": "Whenever you play a card, deal {Amount} damage to ALL enemies."},
        "ChokeHoldPower": {"NAME": "Choked", "DESCRIPTION": "Whenever the player plays a card this turn, this enemy loses {Amount} HP."},
        "CorpseExplosionPower": {"NAME": "Corpse Explosion", "DESCRIPTION": "On death, deal damage equal to Max HP to ALL enemies."},
        "PhantasmalKillerPower": {"NAME": "Phantasmal", "DESCRIPTION": "Deal double damage next turn."},
        "ElectrodynamicsPower_C": {"NAME": "Electro", "DESCRIPTION": "Lightning hits ALL enemies."},
        "StaticDischargePower_C": {"NAME": "Static Discharge", "DESCRIPTION": "Whenever you receive attack damage, Channel {Amount} Lightning."},
        "CreativeAiPower_C": {"NAME": "Creative AI", "DESCRIPTION": "At the start of your turn, add {Amount} random Power cards into your hand."},
        "HelloWorldPower_C": {"NAME": "Hello", "DESCRIPTION": "At the start of your turn, add {Amount} random Common cards into your hand."},
        "StormPower_C": {"NAME": "Storm", "DESCRIPTION": "Whenever you play a Power card, Channel {Amount} Lightning."},
        "MachineLearningPower_C": {"NAME": "Machine Learning", "DESCRIPTION": "At the start of your turn, draw {Amount} additional cards."},
        "SelfRepairPower_C": {"NAME": "Repair", "DESCRIPTION": "At the end of combat, heal {Amount} HP."},
        "LoopPower_C": {"NAME": "Loop", "DESCRIPTION": "At the start of your turn, trigger the passive ability of your next Orb {Amount} times."},
        "HeatsinksPower_C": {"NAME": "Heatsink", "DESCRIPTION": "Whenever you play a Power card, draw {Amount} cards."},
        "LockOnPower_C": {"NAME": "Lock-On", "DESCRIPTION": "Receives 50% more damage from Orbs for {Amount} turn(s)."},
        "ReboundPower_C": {"NAME": "Rebound", "DESCRIPTION": "The next card you play this turn is placed on top of your draw pile."},
        "AmplifyPower_C": {"NAME": "Amplify", "DESCRIPTION": "Your next Power card is played twice this turn."},
        "EchoFormPower_C": {"NAME": "Echo Form", "DESCRIPTION": "The first card you play each turn is played twice."},
        "SadisticNaturePower_C": {"NAME": "Sadistic Nature", "DESCRIPTION": "Whenever you apply a debuff to an enemy, they take {Amount} damage."},
        "MagnetismPower_C": {"NAME": "Magnetism", "DESCRIPTION": "At the start of your turn, add a random colorless card into your hand."},
        "ClassicPlatedArmorPower_C": {
            "NAME": "Plated Armor",
            "DESCRIPTION": "At the end of your turn, gain {Amount} Block. Whenever you lose HP from an attack, this effect loses 1 stack."
        },
    },
    "zhs": {
        "FlexPower": {"NAME": "\u6d3b\u52a8\u808c\u8089", "DESCRIPTION": "\u83b7\u5f97 {Amount} \u70b9\u529b\u91cf\u3002\u56de\u5408\u7ed3\u675f\u65f6\uff0c\u5931\u53bb {Amount} \u70b9\u529b\u91cf\u3002"},
        "CombustPower_C": {"NAME": "\u81ea\u71c3", "DESCRIPTION": "\u56de\u5408\u7ed3\u675f\u65f6\uff0c\u5931\u53bb 1 \u70b9\u751f\u547d\uff0c\u5bf9\u6240\u6709\u654c\u4eba\u9020\u6210 {Amount} \u70b9\u4f24\u5bb3\u3002"},
        "EvolvePower_C": {"NAME": "\u8fdb\u5316", "DESCRIPTION": "\u6bcf\u5f53\u4f60\u62bd\u5230\u72b6\u6001\u724c\u65f6\uff0c\u62bd {Amount} \u5f20\u724c\u3002"},
        "FireBreathingPower_C": {"NAME": "\u706b\u7130\u5410\u606f", "DESCRIPTION": "\u6bcf\u5f53\u4f60\u62bd\u5230\u72b6\u6001\u6216\u8bc5\u5492\u724c\u65f6\uff0c\u5bf9\u6240\u6709\u654c\u4eba\u9020\u6210 {Amount} \u70b9\u4f24\u5bb3\u3002"},
        "MetallicizePower_C": {"NAME": "\u91d1\u5c5e\u5316", "DESCRIPTION": "\u56de\u5408\u7ed3\u675f\u65f6\uff0c\u83b7\u5f97 {Amount} \u70b9\u683c\u6321\u3002"},
        "BerserkPower_C": {"NAME": "\u72c2\u66b4", "DESCRIPTION": "\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002"},
        "BrutalityPower_C": {"NAME": "\u6b8b\u66b4", "DESCRIPTION": "\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u5931\u53bb 1 \u70b9\u751f\u547d\uff0c\u62bd {Amount} \u5f20\u724c\u3002"},
        "RupturePower_C": {"NAME": "\u6495\u88c2", "DESCRIPTION": "\u6bcf\u5f53\u4f60\u56e0\u5361\u724c\u800c\u5931\u53bb\u751f\u547d\u65f6\uff0c\u83b7\u5f97{Amount}\u70b9[gold]\u529b\u91cf[/gold]\u3002"},
        "AThousandCutsPower": {"NAME": "\u51cc\u8fdf", "DESCRIPTION": "\u4f60\u6bcf\u6253\u51fa\u4e00\u5f20\u724c\uff0c\u5c31\u5bf9\u6240\u6709\u654c\u4eba\u9020\u6210 {Amount} \u70b9\u4f24\u5bb3\u3002"},
        "ChokeHoldPower": {"NAME": "\u52d2\u8116", "DESCRIPTION": "\u73a9\u5bb6\u6bcf\u6253\u51fa\u4e00\u5f20\u724c\uff0c\u8be5\u654c\u4eba\u5931\u53bb {Amount} \u70b9\u751f\u547d\u3002"},
        "CorpseExplosionPower": {"NAME": "\u5c38\u7206\u672f", "DESCRIPTION": "\u6b7b\u4ea1\u65f6\uff0c\u5bf9\u6240\u6709\u654c\u4eba\u9020\u6210\u7b49\u540c\u6700\u5927\u751f\u547d\u503c\u7684\u4f24\u5bb3\u3002"},
        "PhantasmalKillerPower": {"NAME": "\u5e7b\u5f71", "DESCRIPTION": "\u4e0b\u4e00\u56de\u5408\u9020\u6210\u53cc\u500d\u4f24\u5bb3\u3002"},
        "ElectrodynamicsPower_C": {"NAME": "\u7535\u52a8\u529b\u5b66", "DESCRIPTION": "\u95ea\u7535\u547d\u4e2d\u6240\u6709\u654c\u4eba\u3002"},
        "StaticDischargePower_C": {"NAME": "\u9759\u7535\u91ca\u653e", "DESCRIPTION": "\u5f53\u4f60\u53d7\u5230[gold]\u653b\u51fb[/gold]\u4f24\u5bb3\u65f6\uff0c\n\u751f\u6210 [blue]{Amount}[/blue]\u4e2a[gold]\u95ea\u7535[/gold]\u5145\u80fd\u7403\u3002"},
        "CreativeAiPower_C": {"NAME": "\u521b\u9020\u6027AI", "DESCRIPTION": "\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u5c06 {Amount} \u5f20\u968f\u673a\u80fd\u529b\u724c\u52a0\u5165\u4f60\u7684\u624b\u724c\u3002"},
        "HelloWorldPower_C": {"NAME": "\u4f60\u597d", "DESCRIPTION": "\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u5c06{Amount}\u5f20\u968f\u673a\u666e\u901a\u724c\u52a0\u5165\u4f60\u7684\u624b\u724c\u3002"},
        "StormPower_C": {"NAME": "\u96f7\u66b4", "DESCRIPTION": "\u6bcf\u5f53\u4f60\u6253\u51fa\u80fd\u529b\u724c\u65f6\uff0c\u751f\u6210 {Amount} \u4e2a\u95ea\u7535\u5145\u80fd\u7403\u3002"},
        "MachineLearningPower_C": {"NAME": "\u673a\u5668\u5b66\u4e60", "DESCRIPTION": "\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u989d\u5916\u62bd {Amount} \u5f20\u724c\u3002"},
        "SelfRepairPower_C": {"NAME": "\u4fee\u7406", "DESCRIPTION": "\u6218\u6597\u7ed3\u675f\u65f6\uff0c\u6062\u590d {Amount} \u70b9\u751f\u547d\u3002"},
        "LoopPower_C": {"NAME": "\u5faa\u73af", "DESCRIPTION": "\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u89e6\u53d1\u4f60\u7684\u4e0b\u4e00\u4e2a\u5145\u80fd\u7403\u7684\u88ab\u52a8\u6548\u679c {Amount} \u6b21\u3002"},
        "HeatsinksPower_C": {"NAME": "\u6563\u70ed", "DESCRIPTION": "\u6bcf\u5f53\u4f60\u6253\u51fa\u80fd\u529b\u724c\u65f6\uff0c\u62bd {Amount} \u5f20\u724c\u3002"},
        "LockOnPower_C": {"NAME": "\u8ddf\u8e2a\u9501\u5b9a", "DESCRIPTION": "\u4ece\u5145\u80fd\u7403\u53d7\u5230\u7684\u4f24\u5bb3\u589e\u52a0 50%\uff0c\u6301\u7eed {Amount} \u56de\u5408\u3002"},
        "ReboundPower_C": {"NAME": "\u5f39\u56de", "DESCRIPTION": "\u4f60\u5728\u8fd9\u4e2a\u56de\u5408\u6253\u51fa\u7684\u4e0b\u4e00\u5f20\u724c\u5c06\u4f1a\u88ab\u653e\u7f6e\u5230\u62bd\u724c\u5806\u7684\u9876\u90e8\u3002"},
        "AmplifyPower_C": {"NAME": "\u589e\u5e45", "DESCRIPTION": "\u5728\u8fd9\u4e2a\u56de\u5408\uff0c\u4f60\u7684\u4e0b\u4e00\u5f20\u80fd\u529b\u724c\u4f1a\u6253\u51fa\u4e24\u6b21\u3002"},
        "EchoFormPower_C": {"NAME": "\u56de\u54cd\u5f62\u6001", "DESCRIPTION": "\u6bcf\u56de\u5408\u4f60\u6253\u51fa\u7684\u7b2c\u4e00\u5f20\u724c\u4f1a\u88ab\u6253\u51fa\u4e24\u6b21\u3002"},
        "SadisticNaturePower_C": {"NAME": "\u6b8b\u8650\u5929\u6027", "DESCRIPTION": "\u6bcf\u5f53\u4f60\u7ed9\u654c\u4eba\u65bd\u52a0\u8d1f\u9762\u72b6\u6001\u65f6\uff0c\u4f7f\u5176\u53d7\u5230 {Amount} \u70b9\u4f24\u5bb3\u3002"},
        "MagnetismPower_C": {"NAME": "\u78c1\u529b", "DESCRIPTION": "\u5728\u4f60\u7684\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u5c061\u5f20\u968f\u673a\u65e0\u8272\u724c\u52a0\u5165\u624b\u724c\u3002"},
        "ClassicPlatedArmorPower_C": {
            "NAME": "\u591a\u5c42\u62a4\u7532",
            "DESCRIPTION": "\u5728\u4f60\u7684\u56de\u5408\u7ed3\u675f\u65f6\u83b7\u5f97 {Amount} \u70b9\u683c\u6321\u3002\u53d7\u5230\u653b\u51fb\u4f24\u5bb3\u800c\u5931\u53bb\u751f\u547d\u65f6\uff0c\u8be5\u6548\u679c\u5c42\u6570\u5c06\u51cf\u5c11 1 \u3002"
        },
    },
}

# Hardcoded card descriptions for ClassicMode-only cards not present in STS1 data
_CUSTOM_CARD_LOC = {
    "eng": {
        "HexaghostBurnPlus": {
            "NAME": "Burn+",
            "DESCRIPTION": "At the end of your turn, if this is in your [gold]Hand[/gold], take {Damage:diff()} damage."
        },
        "FanOfKnives_C": {
            "NAME": "Fan of Knives",
            "DESCRIPTION": "Deal {Damage:diff()} damage to ALL enemies.\nDraw {Cards:diff()} card."
        },
        "Adrenaline_C": {
            "NAME": "Adrenaline",
            "DESCRIPTION": "Gain {Energy:energyIcons()}.\nDraw 2 cards."
        },
        "Storm_C": {
            "NAME": "Storm",
            "DESCRIPTION": "Whenever you play a Power card, Channel {Storm:diff()} Lightning."
        },
        "Loop_C": {
            "NAME": "Loop",
            "DESCRIPTION": "At the start of your turn, trigger the passive ability of your next [gold]Orb[/gold] {Loop:diff()} times."
        },
        "UltimateStrikeEvent_C": {
            "NAME": "Ultimate Strike",
            "DESCRIPTION": "Deal {Damage:diff()} damage."
        },
        "UltimateDefendEvent_C": {
            "NAME": "Ultimate Defend",
            "DESCRIPTION": "Gain {Block:diff()} [gold]Block[/gold]."
        },
    },
    "zhs": {
        "HexaghostBurnPlus": {
            "NAME": "灼伤+",
            "DESCRIPTION": "在你的回合结束时，如果这张牌在你的[gold]手牌[/gold]中，你受到{Damage:diff()}点伤害。"
        },
        "FanOfKnives_C": {
            "NAME": "万刃齐发",
            "DESCRIPTION": "对所有敌人造成{Damage:diff()}点伤害。\n抽{Cards:diff()}张牌。"
        },
        "Adrenaline_C": {
            "NAME": "肾上腺素",
            "DESCRIPTION": "获得{Energy:energyIcons()}。\n抽2张牌。"
        },
        "Storm_C": {
            "NAME": "雷暴",
            "DESCRIPTION": "你每打出一张能力牌，生成{Storm:diff()}个闪电[gold]充能球[/gold]。"
        },
        "Loop_C": {
            "NAME": "循环",
            "DESCRIPTION": "在你的回合开始时，使用你最右侧的1个[gold]充能球[/gold]的被动能力{Loop:diff()}次。"
        },
        "UltimateStrikeEvent_C": {
            "NAME": "究极打击",
            "DESCRIPTION": "造成{Damage:diff()}点伤害。"
        },
        "UltimateDefendEvent_C": {
            "NAME": "究极防御",
            "DESCRIPTION": "获得{Block:diff()}点[gold]格挡[/gold]。"
        },
    },
}

# Extra non-card-model keys that must survive every localization regeneration.
_EXTRA_CARD_LOC = {
    "eng": {
        "CLASSIC_ORIGIN_HOVERTIP.title": "Classic Card",
        "CLASSIC_ORIGIN_HOVERTIP.description": "This card comes from Slay the Spire 1.",
    },
    "zhs": {
        "CLASSIC_ORIGIN_HOVERTIP.title": "一代卡牌",
        "CLASSIC_ORIGIN_HOVERTIP.description": "这张卡来自《杀戮尖塔》一代。",
    },
}

# zh terminology that should be highlighted, and synced to the paired EN text.
_TERM_HIGHLIGHT_MAP = [
    ("\u653b\u51fb", "Attack"),
    ("\u80fd\u529b\u724c", "Power card"),
    ("\u95ea\u7535", "Lightning"),
    ("\u6613\u4f24", "Vulnerable"),
    ("\u865a\u5f31", "Weak"),
    ("\u4e2d\u6bd2", "Poison"),
    ("\u65e0\u5b9e\u4f53", "Intangible"),
    ("\u7f13\u51b2", "Buffer"),
    ("\u8346\u68d8", "Thorns"),
    ("\u518d\u751f", "Regen"),
    ("\u683c\u6321", "Block"),
    ("\u529b\u91cf", "Strength"),
    ("\u654f\u6377", "Dexterity"),
    ("\u96c6\u4e2d", "Focus"),
    ("\u4eba\u5de5\u5236\u54c1", "Artifact"),
    ("\u91d1\u5c5e\u5316", "Plated Armor"),
    ("\u4eea\u5f0f", "Ritual"),
    ("\u5145\u80fd\u7403", "Orb"),
    ("\u6d88\u8017", "Exhaust"),
    ("\u56fa\u6709", "Innate"),
    ("\u865a\u65e0", "Ethereal"),
]

_CJK = r"\u4e00-\u9fff"


def _normalize_zh_description(text: str):
    """Remove unnecessary zh spaces and add [gold] highlight for key terms."""
    v = text or ""
    changed_terms = set()

    # Case 1: remove spaces around numbers/placeholders and CJK units.
    v = re.sub(rf"(?<=[{_CJK}])[ \t]+(?=[0-9{{])", "", v)
    v = re.sub(rf"(?<=[0-9}}])[ \t]+(?=[{_CJK}])", "", v)

    # Generic whitespace cleanup in Chinese sentence flow.
    # Do not consume newlines here: only trim spaces/tabs around zh flow.
    v = re.sub(rf"(?<=\[/gold\])[ \t]+(?=[{_CJK}])", "", v)
    v = re.sub(rf"(?<=[{_CJK}\]）\)])[ \t]+(?=[{_CJK}\[（\(])", "", v)
    v = re.sub(rf"([，。！？；：、])[ \t]+(?=[{_CJK}\[])", r"\1", v)

    # General zh punctuation spacing cleanup.
    v = re.sub(r"[ \t]+([，。！？；：、）\)])", r"\1", v)
    v = re.sub(r"([（\(])[ \t]+", r"\1", v)

    # Case 2: add gold highlight for key terms.
    # We intentionally do not require whitespace boundaries so phrases like
    # "格挡不再..." still highlight "格挡".
    for zh_term, _ in _TERM_HIGHLIGHT_MAP:
        # Allow accidental internal whitespace inside terms (e.g. "敏 捷").
        escaped = r"[ \t]*".join(re.escape(ch) for ch in zh_term)
        # First normalize existing highlighted variants with internal spaces.
        v = re.sub(rf"\[gold\]\s*{escaped}\s*\[/gold\]", f"[gold]{zh_term}[/gold]", v)
        pattern = re.compile(rf"(?<!\[gold\])({escaped})(?!\[/gold\])")
        replaced = pattern.sub(rf"[gold]\1[/gold]", v)
        # Normalize any spaces that were matched inside the highlighted term.
        replaced = re.sub(rf"\[gold\]{escaped}\[/gold\]", f"[gold]{zh_term}[/gold]", replaced)
        if replaced != v:
            changed_terms.add(zh_term)
            v = replaced

    # Final pass: collapse accidental double spaces.
    v = re.sub(r" {2,}", " ", v)
    return v, changed_terms


def _sync_english_highlight(text: str, english_term: str):
    escaped = re.escape(english_term)
    pattern = re.compile(rf"(?<!\[gold\])(?<!\{{)\b{escaped}\b(?!:)(?!\[/gold\])")
    return pattern.sub(f"[gold]{english_term}[/gold]", text)


def _apply_zh_spacing_and_highlight(zhs_cards: dict, eng_cards: dict):
    """Apply zh spacing/highlight rules and sync matching EN term highlights."""
    changed_desc_count = 0
    changed_en_count = 0
    all_changed_terms = set()

    zh_to_en = {zh: en for zh, en in _TERM_HIGHLIGHT_MAP}

    for key in list(zhs_cards.keys()):
        if not (key.endswith(".description") or key.endswith(".upgradedDescription")):
            continue

        original = str(zhs_cards.get(key, ""))
        fixed, changed_terms = _normalize_zh_description(original)

        if fixed != original:
            zhs_cards[key] = fixed
            changed_desc_count += 1

        if changed_terms and key in eng_cards:
            en_text = str(eng_cards.get(key, ""))
            en_fixed = en_text
            for zh_term in changed_terms:
                all_changed_terms.add(zh_term)
                en_term = zh_to_en[zh_term]
                en_fixed = _sync_english_highlight(en_fixed, en_term)

            if en_fixed != en_text:
                eng_cards[key] = en_fixed
                changed_en_count += 1

    terms_text = ", ".join(sorted(all_changed_terms)) if all_changed_terms else "(none)"
    print(f"  zh spacing/highlight changes: {changed_desc_count}")
    print(f"  en highlight sync changes: {changed_en_count}")
    print(f"  terms highlighted: {terms_text}")


def _apply_zh_term_highlight_to_loc_dict(zhs_loc: dict, eng_loc: dict, label: str):
    """Apply zh spacing/highlight and EN sync to generic loc description fields."""
    changed_desc_count = 0
    changed_en_count = 0
    all_changed_terms = set()

    zh_to_en = {zh: en for zh, en in _TERM_HIGHLIGHT_MAP}

    for key in list(zhs_loc.keys()):
        if not (
            key.endswith(".description")
            or key.endswith(".upgradedDescription")
            or key.endswith(".smartDescription")
            or key.endswith(".remoteDescription")
        ):
            continue

        original = str(zhs_loc.get(key, ""))
        fixed, changed_terms = _normalize_zh_description(original)

        if fixed != original:
            zhs_loc[key] = fixed
            changed_desc_count += 1

        if changed_terms and key in eng_loc:
            en_text = str(eng_loc.get(key, ""))
            en_fixed = en_text
            for zh_term in changed_terms:
                all_changed_terms.add(zh_term)
                en_term = zh_to_en[zh_term]
                en_fixed = _sync_english_highlight(en_fixed, en_term)

            if en_fixed != en_text:
                eng_loc[key] = en_fixed
                changed_en_count += 1

    terms_text = ", ".join(sorted(all_changed_terms)) if all_changed_terms else "(none)"
    print(f"  [{label}] zh spacing/highlight changes: {changed_desc_count}")
    print(f"  [{label}] en highlight sync changes: {changed_en_count}")
    print(f"  [{label}] terms highlighted: {terms_text}")


def _normalize_placeholder_case_in_loc_dict(loc: dict, label: str):
    """Fix known unresolved lowercase placeholders like {amount} -> {Amount}."""
    changed = 0

    def fix_text(text: str) -> str:
        if not text:
            return text

        def repl(match: re.Match) -> str:
            key = match.group(1)
            suffix = match.group(2) or ""
            mapped = _PLACEHOLDER_CASE_MAP.get(key.lower(), key)
            return "{" + mapped + suffix + "}"

        return re.sub(r"\{([A-Za-z_][A-Za-z0-9_]*)(:[^}]*)?\}", repl, text)

    for key in list(loc.keys()):
        value = loc.get(key)
        if not isinstance(value, str):
            continue
        fixed = fix_text(value)
        if fixed != value:
            loc[key] = fixed
            changed += 1

    print(f"  [{label}] placeholder case fixes: {changed}")


def _normalize_power_amount_diff(loc: dict, label: str):
    """Normalize power Amount placeholders to a stable blue-highlighted variable style."""
    changed = 0

    for key in list(loc.keys()):
        if not (
            key.endswith(".description")
            or key.endswith(".smartDescription")
            or key.endswith(".remoteDescription")
        ):
            continue

        value = loc.get(key)
        if not isinstance(value, str):
            continue

        # Power tooltip formatting does not reliably resolve `:diff()` in all paths.
        # Render Amount as a normal dynamic variable with explicit blue color,
        # while preserving already-correct blue-wrapped variants.
        marker = "__AMOUNT_BLUE_MARKER__"
        protected = value.replace("[blue]{Amount}[/blue]", marker)
        protected = protected.replace("[blue]{Amount:diff()}[/blue]", marker)
        fixed = re.sub(r"\{Amount(?::diff\(\))?\}", "[blue]{Amount}[/blue]", protected)
        fixed = fixed.replace(marker, "[blue]{Amount}[/blue]")
        if fixed != value:
            loc[key] = fixed
            changed += 1

    print(f"  [{label}] amount diff formatter fixes: {changed}")


def _convert_sts1_markup_to_sts2(text: str) -> str:
    """Convert STS1 inline markers (#y/#b/...) to STS2 rich tags."""
    if not text:
        return ""

    v = text
    v = re.sub(r"#b\s*(\{[^}]+\}|-?\d+)", r"[blue]\1[/blue]", v)
    v = re.sub(r"#y\s*([^\s，。！？；：,.!?]+)", r"[gold]\1[/gold]", v)
    v = re.sub(r"#[byrgp]", "", v)
    for icon in ('[R]', '[G]', '[B]', '[E]', '[W]'):
        v = v.replace(icon, '{energyPrefix:energyIcons(1)}')

    def _collapse_energy(match):
        count = len(re.findall(r'\{energyPrefix:energyIcons\(1\)\}', match.group(0)))
        return f'{{energyPrefix:energyIcons({count})}}'

    v = re.sub(r'(?:\{energyPrefix:energyIcons\(1\)\}\s*){2,}', _collapse_energy, v)
    v = re.sub(r"\bNL\b", "\n", v)
    v = re.sub(r" +", " ", v).strip()
    return v


def _normalize_power_wording(desc: str, lang: str, slug: str) -> str:
    """Apply canonical wording for generated power descriptions."""
    if not desc:
        return ""

    if lang != "zhs":
        return desc

    if slug == "STATIC_DISCHARGE_POWER_C":
        return "当你受到[gold]攻击[/gold]伤害时，\n生成 [blue]{Amount}[/blue]个[gold]闪电[/gold]充能球。"

    if slug == "STORM_POWER_C":
        return "每当你打出[gold]能力牌[/gold]时，生成 [blue]{Amount}[/blue]个[gold]闪电[/gold]充能球。"

    return desc

# Cards whose CanonicalVars declare `RepeatVar` rather than a MagicNumber
# DynamicVar. The STS1 converter emits `{MagicNumber:diff()}` for `!M!`, but
# these cards' DynamicVarSet has no `MagicNumber` — it has `Repeat` — so the
# placeholder leaks through unresolved (e.g. "Damage::diff()"-style garbage)
# on the rendered card. Rename every `MagicNumber` reference in their
# description to `Repeat` post-conversion.
_REPEAT_VAR_CARDS = {
    "SwordBoomerang_C",
    "Pummel_C",
    "BouncingFlask_C",
    "Electrodynamics_C",
    "Chaos_C",
    "Capacitor_C",
}

# Bespoke `selectionScreenPrompt` text for cards that open a CardSelect screen.
# Cards not listed here fall back to the generic "Choose a card" prompt.
_CARD_PROMPTS = {
    "eng": {
        "DualWield_C": "Choose an Attack or Power",
        "Warcry_C": "Place a card on top of the draw pile",
        "Headbutt_C": "Place a card from your discard pile on top of the draw pile",
        "Armaments_C": "Choose a card to upgrade",
        "Exhume_C": "Choose a card to return to your hand",
        "Setup_C": "Place a card on top of the draw pile",
        "Nightmare_C": "Choose a card to add copies of next turn",
        "Recursion_C": "Choose an Orb to evoke",
    },
    "zhs": {
        "DualWield_C": "\u9009\u62e9\u4e00\u5f20\u653b\u51fb\u724c\u6216\u80fd\u529b\u724c",
        "Warcry_C": "\u5c06\u4e00\u5f20\u624b\u724c\u653e\u5230\u62bd\u724c\u5806\u9876\u90e8",
        "Headbutt_C": "\u4ece\u5f03\u724c\u5806\u9009\u4e00\u5f20\u724c\u653e\u5165\u62bd\u724c\u5806\u9876\u90e8",
        "Armaments_C": "\u9009\u62e9\u4e00\u5f20\u724c\u8fdb\u884c\u5347\u7ea7",
        "Exhume_C": "\u9009\u62e9\u4e00\u5f20\u724c\u8fd4\u56de\u624b\u724c",
        "Setup_C": "\u5c06\u4e00\u5f20\u724c\u653e\u5230\u62bd\u724c\u5806\u9876\u90e8",
        "Nightmare_C": "\u9009\u62e9\u4e00\u5f20\u724c\u4e0b\u56de\u5408\u590d\u5236",
        "Recursion_C": "\u9009\u62e9\u4e00\u4e2a\u5145\u80fd\u7403\u8fdb\u884c\u5f15\u5bfc",
    },
}

# Fallback overrides for custom relic localization.
# Primary source is STS1 desktop localization in generate_localization().
_CUSTOM_RELIC_LOC = {
    "eng": {
        "MarkOfPain": {
            "NAME": "Mark of Pain",
            "DESCRIPTION": "Gain {energyPrefix:energyIcons(1)} at the start of your turn. At the start of combat, shuffle 2 Wounds into your draw pile.",
            "FLAVOR": ""
        },
        "ChampionBelt": {
            "NAME": "Champion Belt",
            "DESCRIPTION": "Whenever you apply Vulnerable, apply {WeakAmount} Weak.",
            "FLAVOR": ""
        },
        "WristBlade": {
            "NAME": "Wrist Blade",
            "DESCRIPTION": "Attacks that cost 0 deal {ExtraDamage} additional damage.",
            "FLAVOR": ""
        },
        "HoveringKite": {
            "NAME": "Hovering Kite",
            "DESCRIPTION": "The first time you discard a card each turn, gain {energyPrefix:energyIcons(1)}.",
            "FLAVOR": ""
        },
        "TheSpecimen": {
            "NAME": "The Specimen",
            "DESCRIPTION": "Whenever an enemy dies, transfer any Poison it has to a random enemy.",
            "FLAVOR": ""
        },
        "FrozenCore": {
            "NAME": "Frozen Core",
            "DESCRIPTION": "If you end your turn with any empty [gold]Orb[/gold] slots, [gold]Channel[/gold] [blue]1[/blue] [gold]Frost[/gold].",
            "FLAVOR": ""
        },
        "RingOfTheSerpent": {
            "NAME": "Ring of the Serpent",
            "DESCRIPTION": "Replaces Ring of the Snake. At the start of your turn, draw [blue]1[/blue] additional card.",
            "FLAVOR": ""
        },
        "Inserter": {
            "NAME": "Inserter",
            "DESCRIPTION": "Every 2 turns, gain 1 Orb slot.",
            "FLAVOR": ""
        },
        "NuclearBattery": {
            "NAME": "Nuclear Battery",
            "DESCRIPTION": "At the start of each combat, Channel 1 Plasma.",
            "FLAVOR": ""
        },
        "ClassicBronzeScales": {
            "NAME": "Bronze Scales",
            "DESCRIPTION": "At the start of each combat, gain [blue]{ThornsPower}[/blue] [gold]Thorns[/gold].",
            "FLAVOR": ""
        },
        "CeramicFishRelic": {
            "NAME": "Ceramic Fish",
            "DESCRIPTION": "Whenever you add a card to your deck, gain [blue]{Gold}[/blue] [gold]Gold[/gold].",
            "FLAVOR": ""
        },
        "ToyOrnithopterRelic": {
            "NAME": "Toy Ornithopter",
            "DESCRIPTION": "Whenever you use a potion, heal [blue]{Heal}[/blue] HP.",
            "FLAVOR": ""
        },
        "BirdFacedUrnRelic": {
            "NAME": "Bird-Faced Urn",
            "DESCRIPTION": "Whenever you play a [gold]Power[/gold] card, heal [blue]{Heal}[/blue] HP.",
            "FLAVOR": ""
        },
        "BlueCandleRelic": {
            "NAME": "Blue Candle",
            "DESCRIPTION": "[gold]Unplayable[/gold] Curse cards can now be played.\n\nWhenever you play a Curse, lose [blue]1[/blue] HP and [gold]Exhaust[/gold] it.",
            "FLAVOR": ""
        },
        "CalipersRelic": {
            "NAME": "Calipers",
            "DESCRIPTION": "At the start of your turn, lose [blue]{BlockLoss}[/blue] [gold]Block[/gold] rather than all of your [gold]Block[/gold].",
            "FLAVOR": ""
        },
        "ClassicNunchaku": {
            "NAME": "Nunchaku",
            "DESCRIPTION": "Every time you play [blue]{Cards}[/blue] [gold]Attacks[/gold], gain {energyPrefix:energyIcons(1)}.",
            "FLAVOR": ""
        },
        "DuVuDollRelic": {
            "NAME": "Du-Vu Doll",
            "DESCRIPTION": "For each Curse in your deck, start each combat with [blue]1[/blue] [gold]Strength[/gold].",
            "FLAVOR": ""
        },
        "PreservedInsectRelic": {
            "NAME": "Preserved Insect",
            "DESCRIPTION": "Enemies in Elite combats have [blue]{HpReductionPercent}[/blue]% less HP.",
            "FLAVOR": ""
        },
        "SmilingMaskRelic": {
            "NAME": "Smiling Mask",
            "DESCRIPTION": "The Merchant's card removal service now always costs [blue]50[/blue] [gold]Gold[/gold].",
            "FLAVOR": ""
        },
        "SundialRelic": {
            "NAME": "Sundial",
            "DESCRIPTION": "Every [blue]{Shuffles}[/blue] times you shuffle your draw pile, gain {energyPrefix:energyIcons(2)}.",
            "FLAVOR": ""
        },
        "ThreadAndNeedleRelic": {
            "NAME": "Thread and Needle",
            "DESCRIPTION": "Start each combat with [blue]4[/blue] Plated Armor.",
            "FLAVOR": ""
        },
        "TinyChestRelic": {
            "NAME": "Tiny Chest",
            "DESCRIPTION": "Every [blue]4[/blue]th ? room is a [gold]Treasure[/gold] room.",
            "FLAVOR": ""
        },
        "MatryoshkaRelic": {
            "NAME": "Matryoshka",
            "DESCRIPTION": "The next [blue]{ChestsLeft}[/blue] non-Boss chests you open contain [blue]2[/blue] [gold]Relics[/gold].",
            "FLAVOR": ""
        },
        "OmamoriRelic": {
            "NAME": "Omamori",
            "DESCRIPTION": "Negate the next [blue]{Negates}[/blue] Curses you obtain.",
            "FLAVOR": ""
        },
        "ToriiRelic": {
            "NAME": "Torii",
            "DESCRIPTION": "Whenever you would receive [blue]5[/blue] or less unblocked [gold]Attack[/gold] damage, reduce it to [blue]1[/blue].",
            "FLAVOR": ""
        },
        "BottledFlameRelic": {
            "NAME": "Bottled Flame",
            "DESCRIPTION": "Upon pickup, choose an [gold]Attack[/gold] in your [gold]Deck[/gold] to [gold]Enchant[/gold] with [purple]{Enchantment}[/purple].",
            "FLAVOR": ""
        },
        "BottledLightningRelic": {
            "NAME": "Bottled Lightning",
            "DESCRIPTION": "Upon pickup, choose a [gold]Skill[/gold] in your [gold]Deck[/gold] to [gold]Enchant[/gold] with [purple]{Enchantment}[/purple].",
            "FLAVOR": ""
        },
        "BottledTornadoRelic": {
            "NAME": "Bottled Tornado",
            "DESCRIPTION": "Upon pickup, choose a [gold]Power[/gold] in your [gold]Deck[/gold] to [gold]Enchant[/gold] with [purple]{Enchantment}[/purple].",
            "FLAVOR": ""
        },
        "BustedCrownRelic": {
            "NAME": "Busted Crown",
            "DESCRIPTION": "Gain {energyPrefix:energyIcons(1)} at the start of your turn. Future card rewards have [blue]2[/blue] fewer cards to choose from.",
            "FLAVOR": "The Champ's crown... or a pale imitation?"
        },
        "CoffeeDripperRelic": {
            "NAME": "Coffee Dripper",
            "DESCRIPTION": "Gain {energyPrefix:energyIcons(1)} at the start of your turn. You can no longer [gold]Rest[/gold] at Rest Sites.",
            "FLAVOR": "\"Yes, another cup please. Back to work. Back to work!\" - The Architect"
        },
        "CursedKeyRelic": {
            "NAME": "Cursed Key",
            "DESCRIPTION": "Gain {energyPrefix:energyIcons(1)} at the start of your turn. Whenever you open a non-Boss chest, obtain a Curse.",
            "FLAVOR": "You can feel the malicious energy emanating from the key. Power comes at a price."
        },
        "FusionHammerRelic": {
            "NAME": "Fusion Hammer",
            "DESCRIPTION": "Gain {energyPrefix:energyIcons(1)} at the start of your turn. You can no longer [gold]Smith[/gold] at Rest Sites.",
            "FLAVOR": "Once wielded, the owner can never let go."
        },
        "RunicCubeRelic": {
            "NAME": "Runic Cube",
            "DESCRIPTION": "Whenever you lose HP, draw [blue]1[/blue] card.",
            "FLAVOR": "The runes are indecipherable."
        },
        "RunicDomeRelic": {
            "NAME": "Runic Dome",
            "DESCRIPTION": "Gain {energyPrefix:energyIcons(1)} at the start of your turn. You can no longer see enemy intents.",
            "FLAVOR": "The runes are indecipherable."
        },
        "SacredBarkRelic": {
            "NAME": "Sacred Bark",
            "DESCRIPTION": "Double the effectiveness of potions.",
            "FLAVOR": "A bark rumored to originate from the World tree."
        },
        "SlaversCollarRelic": {
            "NAME": "Slaver's Collar",
            "DESCRIPTION": "During Boss and Elite combats, gain {energyPrefix:energyIcons(1)} at the start of your turn.",
            "FLAVOR": "Rusty miserable chains."
        },
        "TinyHouseRelic": {
            "NAME": "Tiny House",
            "DESCRIPTION": "Upon pickup, obtain [blue]1[/blue] potion. \\n Gain [blue]{Gold}[/blue] [gold]Gold[/gold]. \\n Raise your Max HP by [blue]{MaxHp}[/blue]. \\n Obtain [blue]1[/blue] card. \\n Upgrade [blue]1[/blue] random card.",
            "FLAVOR": "\"A near perfect implementation of miniaturization. My finest work to date, but still not adequate.\" - The Architect"
        },
    },
    "zhs": {
        "MarkOfPain": {
            "NAME": "\u75db\u695a\u5370\u8bb0",
            "DESCRIPTION": "\u6bcf\u56de\u5408\u5f00\u59cb\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002\u5728\u6218\u6597\u5f00\u59cb\u65f6\uff0c\u5c062\u5f20\u4f24\u53e3\u653e\u5165\u4f60\u7684\u62bd\u724c\u5806\u3002",
            "FLAVOR": ""
        },
        "ChampionBelt": {
            "NAME": "\u51a0\u519b\u8170\u5e26",
            "DESCRIPTION": "\u6bcf\u5f53\u4f60\u7ed9\u4e88\u6613\u4f24\u65f6\uff0c\u540c\u65f6\u7ed9\u4e88{WeakAmount}\u5c42\u865a\u5f31\u3002",
            "FLAVOR": ""
        },
        "WristBlade": {
            "NAME": "\u8896\u5251",
            "DESCRIPTION": "\u8d39\u7528\u4e3a0\u7684\u653b\u51fb\u724c\u989d\u5916\u9020\u6210{ExtraDamage}\u70b9\u4f24\u5bb3\u3002",
            "FLAVOR": ""
        },
        "HoveringKite": {
            "NAME": "\u60ac\u6d6e\u98ce\u7b5d",
            "DESCRIPTION": "\u4f60\u5728\u6bcf\u56de\u5408\u7b2c\u4e00\u6b21\u5f03\u724c\u65f6\uff0c\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002",
            "FLAVOR": ""
        },
        "TheSpecimen": {
            "NAME": "\u751f\u7269\u6837\u672c",
            "DESCRIPTION": "\u6bcf\u5f53\u6709\u654c\u4eba\u6b7b\u53bb\u65f6\uff0c\u5c06\u5176\u8eab\u4e0a\u7684\u4e2d\u6bd2\u5c42\u6570\u79fb\u5230\u4e00\u540d\u968f\u673a\u654c\u4eba\u8eab\u4e0a\u3002",
            "FLAVOR": ""
        },
        "FrozenCore": {
            "NAME": "\u51bb\u7ed3\u6838\u5fc3",
            "DESCRIPTION": "\u66ff\u6362\u7834\u635f\u6838\u5fc3\u3002\u5982\u679c\u4f60\u5728\u4f60\u7684\u56de\u5408\u7ed3\u675f\u65f6\u8fd8\u6709\u7a7a\u4f59\u7684[gold]\u5145\u80fd\u7403[/gold]\u680f\u4f4d\uff0c\u751f\u6210[blue]1[/blue]\u4e2a[gold]\u51b0\u971c[/gold][gold]\u5145\u80fd\u7403[/gold]\u3002",
            "FLAVOR": ""
        },
        "RingOfTheSerpent": {
            "NAME": "\u957f\u86c7\u6212\u6307",
            "DESCRIPTION": "\u66ff\u6362\u86c7\u4e4b\u6212\u6307\u3002\u5728\u4f60\u7684\u6bcf\u4e2a\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u989d\u5916\u62bd[blue]1[/blue]\u5f20\u724c\u3002",
            "FLAVOR": ""
        },
        "Inserter": {
            "NAME": "\u673a\u68b0\u81c2",
            "DESCRIPTION": "\u6bcf2\u56de\u5408\uff0c\u83b7\u5f971\u4e2a\u5145\u80fd\u7403\u680f\u4f4d\u3002",
            "FLAVOR": ""
        },
        "NuclearBattery": {
            "NAME": "\u6838\u80fd\u7535\u6c60",
            "DESCRIPTION": "\u5728\u6bcf\u573a\u6218\u6597\u5f00\u59cb\u65f6\uff0c\u751f\u62101\u4e2a\u7b49\u79bb\u5b50\u5145\u80fd\u7403\u3002",
            "FLAVOR": ""
        },
        "ClassicBronzeScales": {
            "NAME": "\u94dc\u5236\u9cde\u7247",
            "DESCRIPTION": "\u5728\u6bcf\u573a\u6218\u6597\u5f00\u59cb\u65f6\uff0c\u83b7\u5f97[blue]{ThornsPower}[/blue]\u70b9[gold]\u8346\u68d8[/gold]\u3002",
            "FLAVOR": ""
        },
        "CeramicFishRelic": {
            "NAME": "\u9676\u74f7\u5c0f\u9c7c",
            "DESCRIPTION": "\u6bcf\u6b21\u4f60\u5f80\u81ea\u5df1\u7684\u724c\u7ec4\u4e2d\u52a0\u5165\u4e00\u5f20\u5361\u724c\u65f6\uff0c\u83b7\u5f97[blue]{Gold}[/blue][gold]\u91d1\u5e01[/gold]\u3002",
            "FLAVOR": ""
        },
        "ToyOrnithopterRelic": {
            "NAME": "\u73a9\u5177\u6251\u7ffc\u98de\u673a",
            "DESCRIPTION": "\u4f60\u6bcf\u4f7f\u7528\u4e00\u74f6\u836f\u6c34\uff0c\u56de\u590d[blue]{Heal}[/blue]\u70b9\u751f\u547d\u3002",
            "FLAVOR": ""
        },
        "BirdFacedUrnRelic": {
            "NAME": "\u9e1f\u9762\u74ee",
            "DESCRIPTION": "\u6bcf\u5f53\u4f60\u6253\u51fa\u4e00\u5f20[gold]\u80fd\u529b\u724c[/gold]\uff0c\u56de\u590d[blue]{Heal}[/blue]\u70b9\u751f\u547d\u3002",
            "FLAVOR": ""
        },
        "BlueCandleRelic": {
            "NAME": "\u84dd\u8721\u70db",
            "DESCRIPTION": "\u53ef\u4ee5\u6253\u51fa\u539f\u672c\u4e0d\u80fd\u88ab\u6253\u51fa\u7684[gold]\u8bc5\u5492\u724c[/gold]\uff0c\n\n\u6253\u51fa[gold]\u8bc5\u5492\u724c[/gold]\u4f1a\u8ba9\u4f60\u5931\u53bb[blue]1[/blue]\u70b9\u751f\u547d\u5e76\u5c06\u5176[gold]\u6d88\u8017[/gold]\u3002",
            "FLAVOR": ""
        },
        "CalipersRelic": {
            "NAME": "\u5916\u5361\u94b3",
            "DESCRIPTION": "\u5728\u4f60\u7684\u56de\u5408\u5f00\u59cb\u65f6\uff0c\u4e0d\u518d\u5931\u53bb\u6240\u6709[gold]\u683c\u6321[/gold]\uff0c\u800c\u662f\u5931\u53bb[blue]{BlockLoss}[/blue]\u70b9[gold]\u683c\u6321[/gold]\u3002",
            "FLAVOR": ""
        },
        "ClassicNunchaku": {
            "NAME": "\u53cc\u8282\u68cd",
            "DESCRIPTION": "\u4f60\u6bcf\u6253\u51fa[blue]{Cards}[/blue]\u5f20[gold]\u653b\u51fb\u724c[/gold]\uff0c\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002",
            "FLAVOR": ""
        },
        "DuVuDollRelic": {
            "NAME": "\u6bd2\u5deb\u5a03\u5a03",
            "DESCRIPTION": "\u4f60\u7684\u724c\u7ec4\u4e2d\u6bcf\u6709\u4e00\u5f20[gold]\u8bc5\u5492[/gold]\u724c\uff0c\u4f60\u5728\u6218\u6597\u5f00\u59cb\u65f6\u5c31\u989d\u5916\u83b7\u5f97[blue]1[/blue]\u70b9[gold]\u529b\u91cf[/gold]\u3002",
            "FLAVOR": ""
        },
        "PreservedInsectRelic": {
            "NAME": "\u6606\u866b\u6807\u672c",
            "DESCRIPTION": "\u7cbe\u82f1\u6218\u4e2d\u7684\u654c\u4eba\u751f\u547d\u51cf\u5c11[blue]{HpReductionPercent}[/blue]%\u3002",
            "FLAVOR": ""
        },
        "SmilingMaskRelic": {
            "NAME": "\u5fae\u7b11\u9762\u5177",
            "DESCRIPTION": "\u5546\u4eba\u7684\u5361\u724c\u79fb\u9664\u670d\u52a1\u73b0\u5728\u4ef7\u683c\u6c38\u8fdc\u662f[blue]50[/blue][gold]\u91d1\u5e01[/gold]\u3002",
            "FLAVOR": ""
        },
        "SundialRelic": {
            "NAME": "\u65e5\u6677",
            "DESCRIPTION": "\u4f60\u6bcf\u6d17\u724c[blue]{Shuffles}[/blue]\u6b21\uff0c\u83b7\u5f97{energyPrefix:energyIcons(2)}\u3002",
            "FLAVOR": ""
        },
        "ThreadAndNeedleRelic": {
            "NAME": "\u9488\u7ebf",
            "DESCRIPTION": "\u5728\u6bcf\u573a\u6218\u6597\u5f00\u59cb\u65f6\uff0c\u83b7\u5f97[blue]4[/blue]\u5c42\u591a\u5c42\u62a4\u7532\u3002",
            "FLAVOR": ""
        },
        "TinyChestRelic": {
            "NAME": "\u5c0f\u5b9d\u7bb1",
            "DESCRIPTION": "\u6bcf[blue]4[/blue]\u4e2a\uff1f\u623f\u95f4\u7684\u6700\u540e\u4e00\u4e2a\u5fc5\u662f[gold]\u5b9d\u7bb1\u623f[/gold]\u3002",
            "FLAVOR": ""
        },
        "MatryoshkaRelic": {
            "NAME": "\u5957\u5a03",
            "DESCRIPTION": "\u4f60\u63a5\u4e0b\u6765\u6253\u5f00\u7684[blue]{ChestsLeft}[/blue]\u4e2a\u975e Boss \u5b9d\u7bb1\u5185\u4f1a\u6709[blue]2[/blue]\u4ef6[gold]\u9057\u7269[/gold]\u3002",
            "FLAVOR": ""
        },
        "OmamoriRelic": {
            "NAME": "\u5fa1\u5b88",
            "DESCRIPTION": "\u62b5\u6d88\u4f60\u4e0b[blue]{Negates}[/blue]\u6b21\u83b7\u5f97\u7684\u8bc5\u5492\u3002",
            "FLAVOR": ""
        },
        "ToriiRelic": {
            "NAME": "\u9e1f\u5c45",
            "DESCRIPTION": "\u6bcf\u5f53\u4f60\u53d7\u5230\u5c0f\u4e8e\u7b49\u4e8e[blue]5[/blue]\u70b9\u7684\u672a\u88ab[gold]\u683c\u6321[/gold][gold]\u653b\u51fb[/gold]\u4f24\u5bb3\u65f6\uff0c\u5c06\u4f24\u5bb3\u964d\u4f4e\u4e3a[blue]1[/blue]\u3002",
            "FLAVOR": ""
        },
        "BottledFlameRelic": {
            "NAME": "\u706b\u7130\u74f6",
            "DESCRIPTION": "\u62fe\u8d77\u65f6\uff0c\u4ece[gold]\u724c\u7ec4[/gold]\u4e2d\u9009\u62e9\u4e00\u5f20[gold]\u653b\u51fb\u724c[/gold]\uff0c\u4e3a\u5b83[gold]\u9644\u9b54[/gold]\uff1a[purple]{Enchantment}[/purple]\u3002",
            "FLAVOR": ""
        },
        "BottledLightningRelic": {
            "NAME": "\u95ea\u7535\u74f6",
            "DESCRIPTION": "\u62fe\u8d77\u65f6\uff0c\u4ece[gold]\u724c\u7ec4[/gold]\u4e2d\u9009\u62e9\u4e00\u5f20[gold]\u6280\u80fd\u724c[/gold]\uff0c\u4e3a\u5b83[gold]\u9644\u9b54[/gold]\uff1a[purple]{Enchantment}[/purple]\u3002",
            "FLAVOR": ""
        },
        "BottledTornadoRelic": {
            "NAME": "\u98ce\u66b4\u74f6",
            "DESCRIPTION": "\u62fe\u8d77\u65f6\uff0c\u4ece[gold]\u724c\u7ec4[/gold]\u4e2d\u9009\u62e9\u4e00\u5f20[gold]\u80fd\u529b\u724c[/gold]\uff0c\u4e3a\u5b83[gold]\u9644\u9b54[/gold]\uff1a[purple]{Enchantment}[/purple]\u3002",
            "FLAVOR": ""
        },
        "BustedCrownRelic": {
            "NAME": "\u7834\u788e\u91d1\u51a0",
            "DESCRIPTION": "\u5728\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002\u5728\u5361\u724c\u5956\u52b1\u753b\u9762\uff0c\u53ef\u4f9b\u9009\u62e9\u7684\u724c\u6570\u51cf\u5c11[blue]2[/blue]\u5f20\u3002",
            "FLAVOR": "\u7b2c\u4e00\u52c7\u58eb\u7684\u91d1\u51a0\u2026\u2026\u6216\u8005\u53ea\u662f\u4e00\u4e2a\u62d9\u52a3\u7684\u4eff\u5236\u54c1\uff1f"
        },
        "CoffeeDripperRelic": {
            "NAME": "\u5496\u5561\u6ee4\u676f",
            "DESCRIPTION": "\u5728\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002\u4f60\u65e0\u6cd5\u518d\u5728\u4f11\u606f\u5904[gold]\u4f11\u606f[/gold]\u3002",
            "FLAVOR": "\u201c\u597d\u7684\uff0c\u8bf7\u518d\u6765\u4e00\u676f\u3002\u7ee7\u7eed\u5de5\u4f5c\uff0c\u7ee7\u7eed\u5de5\u4f5c\uff01\u201d\u2014\u2014\u5efa\u7b51\u5e08"
        },
        "CursedKeyRelic": {
            "NAME": "\u8bc5\u5492\u94a5\u5319",
            "DESCRIPTION": "\u5728\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002\u4f60\u6bcf\u6b21\u6253\u5f00\u4e00\u4e2a\u975eBoss\u5b9d\u7bb1\uff0c\u90fd\u4f1a\u83b7\u5f97\u4e00\u5f20\u8bc5\u5492\u3002",
            "FLAVOR": "\u4f60\u80fd\u611f\u5230\u8fd9\u628a\u94a5\u5319\u4e0a\u6563\u53d1\u51fa\u90aa\u6076\u7684\u80fd\u91cf\u3002\u529b\u91cf\u603b\u662f\u9700\u8981\u4ed8\u51fa\u4ee3\u4ef7\u7684\u3002"
        },
        "FusionHammerRelic": {
            "NAME": "\u878d\u5408\u4e4b\u9524",
            "DESCRIPTION": "\u5728\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002\u4f60\u65e0\u6cd5\u518d\u5728\u4f11\u606f\u5904\u953b\u9020\u5347\u7ea7\u5361\u724c\u3002",
            "FLAVOR": "\u4e00\u65e6\u63e1\u4f4f\uff0c\u5c31\u6c38\u8fdc\u65e0\u6cd5\u653e\u4e0b\u3002"
        },
        "RunicCubeRelic": {
            "NAME": "\u7b26\u6587\u7acb\u65b9\u4f53",
            "DESCRIPTION": "\u6bcf\u5f53\u4f60\u5931\u53bb\u751f\u547d\u65f6\uff0c\u62bd[blue]1[/blue]\u5f20\u724c\u3002",
            "FLAVOR": "\u4e0a\u9762\u7684\u7b26\u6587\u65e0\u6cd5\u8fa8\u8bc6\u3002"
        },
        "RunicDomeRelic": {
            "NAME": "\u7b26\u6587\u5706\u9876",
            "DESCRIPTION": "\u5728\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002\u4f60\u65e0\u6cd5\u518d\u770b\u89c1\u654c\u4eba\u7684\u610f\u56fe\u3002",
            "FLAVOR": "\u4e0a\u9762\u7684\u7b26\u6587\u65e0\u6cd5\u8fa8\u8bc6\u3002"
        },
        "SacredBarkRelic": {
            "NAME": "\u795e\u5723\u6811\u76ae",
            "DESCRIPTION": "\u836f\u6c34\u7684\u6548\u679c\u7ffb\u500d\u3002",
            "FLAVOR": "\u4f20\u8bf4\u4e2d\u6765\u81ea\u4e8e\u4e16\u754c\u6811\u4e0a\u7684\u4e00\u5757\u6811\u76ae\u3002"
        },
        "SlaversCollarRelic": {
            "NAME": "\u5974\u96b6\u8d29\u5b50\u9888\u73af",
            "DESCRIPTION": "\u5728Boss\u6218\u4e0e\u7cbe\u82f1\u6218\u4e2d\uff0c\u4f60\u5728\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u83b7\u5f97{energyPrefix:energyIcons(1)}\u3002",
            "FLAVOR": "\u9508\u8680\u53ef\u618e\u7684\u94c1\u94fe\u3002"
        },
        "TinyHouseRelic": {
            "NAME": "\u5c0f\u5c4b\u5b50",
            "DESCRIPTION": "\u62fe\u8d77\u65f6\uff0c\u83b7\u5f97[blue]1[/blue]\u74f6\u836f\u6c34\u3002 \\n \u83b7\u5f97[blue]{Gold}[/blue][gold]\u91d1\u5e01[/gold]\u3002 \\n \u5c06\u4f60\u7684\u6700\u5927\u751f\u547d\u503c\u63d0\u5347[blue]{MaxHp}[/blue]\u3002 \\n \u83b7\u5f97[blue]1[/blue]\u5f20\u724c\u3002 \\n \u968f\u673a\u5347\u7ea7[blue]1[/blue]\u5f20\u724c\u3002",
            "FLAVOR": "\u201c\u4e00\u9879\u8fd1\u4e4e\u5b8c\u7f8e\u7684\u5fae\u7f29\u5316\u5de5\u7a0b\u3002\u8fd9\u662f\u6211\u81f3\u4eca\u6700\u4f18\u79c0\u7684\u4f5c\u54c1\uff0c\u4f46\u4ecd\u7136\u8fdc\u8fdc\u4e0d\u591f\u3002\u201d\u2014\u2014\u5efa\u7b51\u5e08"
        },
    },
}


_CUSTOM_ENCHANTMENT_LOC = {
    "eng": {
        "BottledEnchantment": {
            "NAME": "Bottled",
            "DESCRIPTION": "This card has [gold]Innate[/gold].",
        },
    },
    "zhs": {
        "BottledEnchantment": {
            "NAME": "\u88c5\u74f6",
            "DESCRIPTION": "\u8fd9\u5f20\u724c\u62e5\u6709[gold]\u56fa\u6709[/gold]\u3002",
        },
    },
}

_FORCE_CUSTOM_RELIC_LOCALIZATION = {
    "ClassicBronzeScales",
    "CeramicFishRelic",
    "ToyOrnithopterRelic",
    "BirdFacedUrnRelic",
    "BlueCandleRelic",
    "CalipersRelic",
    "ClassicNunchaku",
    "DuVuDollRelic",
    "PreservedInsectRelic",
    "SmilingMaskRelic",
    "SundialRelic",
    "ThreadAndNeedleRelic",
    "TinyChestRelic",
    "MatryoshkaRelic",
    "OmamoriRelic",
    "ToriiRelic",
    "BottledFlameRelic",
    "BottledLightningRelic",
    "BottledTornadoRelic",
    "BustedCrownRelic",
    "CoffeeDripperRelic",
    "CursedKeyRelic",
    "FusionHammerRelic",
    "RunicCubeRelic",
    "RunicDomeRelic",
    "SacredBarkRelic",
    "SlaversCollarRelic",
    "TinyHouseRelic",
    "FrozenCore",
    "RingOfTheSerpent",
}

# Some runtime paths can resolve relic localization by either class-derived id
# (e.g. BUSTED_CROWN_RELIC) or custom/camel id (e.g. bustedCrown).
# Keep both key styles available for these relics.
_RELIC_LOC_KEY_ALIASES = {
    "BUSTED_CROWN_RELIC": "bustedCrown",
    "COFFEE_DRIPPER_RELIC": "coffeeDripper",
    "CURSED_KEY_RELIC": "cursedKey",
    "FUSION_HAMMER_RELIC": "fusionHammer",
    "RUNIC_CUBE_RELIC": "runicCube",
    "RUNIC_DOME_RELIC": "runicDome",
    "SACRED_BARK_RELIC": "sacredBark",
    "SLAVERS_COLLAR_RELIC": "slaversCollar",
    "TINY_HOUSE_RELIC": "tinyHouse",
}


# Class-name to STS1 relic ID overrides used by generate_localization().
# If not listed, class name is matched against STS1 keys using _normalize().
_CUSTOM_RELIC_STS1_ID_OVERRIDES = {
    # Existing custom relics
    "TheSpecimen": "TheSpecimen",
    "ChampionBelt": "ChampionBelt",
    "MarkOfPain": "Mark of Pain",
    "WristBlade": "Wrist Blade",
    "HoveringKite": "Hovering Kite",
    "RingOfTheSerpent": "Ring of the Serpent",
    "FrozenCore": "Frozen Core",
    "NuclearBattery": "Nuclear Battery",
    # Shared/common/uncommon/rare/shop relics
    "ClassicBronzeScales": "Bronze Scales",
    "OmamoriRelic": "Omamori",
    "SmilingMaskRelic": "Smiling Mask",
    "TinyChestRelic": "Tiny Chest",
    "ClassicNunchaku": "Nunchaku",
    "PreservedInsectRelic": "PreservedInsect",
    "CeramicFishRelic": "CeramicFish",
    "ToyOrnithopterRelic": "Toy Ornithopter",
    "BlueCandleRelic": "Blue Candle",
    "BottledFlameRelic": "Bottled Flame",
    "BottledLightningRelic": "Bottled Lightning",
    "BottledTornadoRelic": "Bottled Tornado",
    "MatryoshkaRelic": "Matryoshka",
    "QuestionCardRelic": "Question Card",
    "SingingBowlRelic": "Singing Bowl",
    "SundialRelic": "Sundial",
    "IncenseBurnerRelic": "Incense Burner",
    "FossilizedHelixRelic": "FossilizedHelix",
    "BirdFacedUrnRelic": "Bird Faced Urn",
    "CalipersRelic": "Calipers",
    "DuVuDollRelic": "Du-Vu Doll",
    "DeadBranchRelic": "Dead Branch",
    "GingerRelic": "Ginger",
    "MagicFlowerRelic": "Magic Flower",
    "PeacePipeRelic": "Peace Pipe",
    "TurnipRelic": "Turnip",
    "ThreadAndNeedleRelic": "Thread and Needle",
    "ToriiRelic": "Torii",
    "FrozenEyeRelic": "Frozen Eye",
    "MedicalKitRelic": "Medical Kit",
    "StrangeSpoonRelic": "Strange Spoon",
    "ClockworkSouvenirRelic": "ClockworkSouvenir",
    "OrangePelletsRelic": "OrangePellets",
    "PrismaticShardRelic": "PrismaticShard",
}


def _slugify(class_name):
    """Convert CamelCase class name to UPPER_SNAKE_CASE model key.

    This must match STS2's ModelDb ID generation:
    e.g. LimitBreak_C → LIMIT_BREAK_C, Bash_C → BASH_C
    """
    out = []
    for i, ch in enumerate(class_name):
        if i > 0 and ch.isupper() and class_name[i - 1].isalnum() and class_name[i - 1] != '_':
            out.append('_')
        out.append(ch)
    return ''.join(out).upper()


def _normalize(name):
    """Normalize a name for fuzzy matching: lowercase, strip spaces/underscores/hyphens."""
    return name.lower().replace(" ", "").replace("_", "").replace("-", "").replace("'", "").replace(".", "")


def _convert_card_desc(desc):
    """Convert STS1 card description to STS2 flat localization format.

    STS2 mod loc uses: {Var:diff()} for card dynamic vars, \\n for newlines,
    [gold]...[/gold] for keywords.
    """
    if not desc:
        return ""

    # Dynamic var replacements (SmartFormat syntax with :diff() for upgrade display)
    desc = desc.replace("!D!", "{Damage:diff()}")
    desc = desc.replace("!B!", "{Block:diff()}")

    # !M! must map to the correct DynamicVar name (PowerVar<XPower> registers as "XPower")
    # Context-aware replacement based on surrounding keyword
    magic_patterns = [
        (r'!M!(\s*)Vulnerable', r'{VulnerablePower:diff()}\1Vulnerable'),
        (r'!M!(\s*)Weak', r'{WeakPower:diff()}\1Weak'),
        (r'!M!(\s*)Poison', r'{PoisonPower:diff()}\1Poison'),
        (r'!M!(\s*)Strength', r'{StrengthPower:diff()}\1Strength'),
        (r'!M!(\s*)Dexterity', r'{DexterityPower:diff()}\1Dexterity'),
        (r'!M!(\s*)Focus', r'{FocusPower:diff()}\1Focus'),
        (r'!M!(\s*)Thorns', r'{ThornsPower:diff()}\1Thorns'),
        (r'!M!(\s*)Artifact', r'{ArtifactPower:diff()}\1Artifact'),
        (r'!M!(\s*)Plated Armor', r'{PlatedArmorPower:diff()}\1Plated Armor'),
        (r'!M!(\s*)Block', r'{Block:diff()}\1Block'),
        (r'!M!(\s*)(cards?)\b', r'{Cards:diff()}\1\2'),
    ]
    for pat, repl in magic_patterns:
        desc = re.sub(pat, repl, desc, flags=re.IGNORECASE)

    # Chinese context patterns for !M!
    zhs_magic = [
        ('!M!(\\s*)((?:层\\s*)?)易伤', '{VulnerablePower:diff()}\\1\\2易伤'),
        ('!M!(\\s*)((?:层\\s*)?)虚弱', '{WeakPower:diff()}\\1\\2虚弱'),
        ('!M!(\\s*)((?:层\\s*)?)中毒', '{PoisonPower:diff()}\\1\\2中毒'),
        ('!M!(\\s*)((?:点\\s*)?)力量', '{StrengthPower:diff()}\\1\\2力量'),
        ('!M!(\\s*)((?:点\\s*)?)敏捷', '{DexterityPower:diff()}\\1\\2敏捷'),
        ('!M!(\\s*)((?:点\\s*)?)集中', '{FocusPower:diff()}\\1\\2集中'),
        ('!M!(\\s*)(?:层\\s*)?荆棘', '{ThornsPower:diff()}\\1荆棘'),
        ('!M!(\\s*)张牌', '{Cards:diff()}\\1张牌'),
    ]
    for pat, repl in zhs_magic:
        desc = re.sub(pat, repl, desc)

    # Remaining !M! → {MagicNumber:diff()} (for cards that define MagicVar)
    desc = desc.replace("!M!", "{MagicNumber:diff()}")

    # Newline: NL → \n (literal newline in JSON string)
    desc = re.sub(r'\bNL\b', '\n', desc)

    # Energy icons
    for icon in ('[R]', '[G]', '[B]', '[E]', '[W]'):
        desc = desc.replace(icon, '{energyPrefix:energyIcons(1)}')

    # Collapse repeated energy placeholders into a single count-based placeholder.
    # Example: "{...1} {...1} {...1}" -> "{energyPrefix:energyIcons(3)}".
    def _collapse_energy(match):
        count = len(re.findall(r'\{energyPrefix:energyIcons\(1\)\}', match.group(0)))
        return f'{{energyPrefix:energyIcons({count})}}'

    desc = re.sub(r'(?:\{energyPrefix:energyIcons\(1\)\}\s*){2,}', _collapse_energy, desc)

    # Remove STS1 formatting codes (#b = bold, #y = yellow/keyword, #r = red)
    desc = re.sub(r'#[byrgp]', '', desc)

    # Remove * bold markers
    desc = desc.replace('*', '')

    # Clean up newline spacing
    desc = re.sub(r' *\n *', '\n', desc)

    # Clean whitespace
    desc = re.sub(r' +', ' ', desc).strip()

    return desc


def _strip_auto_keyword_sentences(desc: str) -> str:
    """Remove standalone auto-rendered keyword sentences from STS1 descriptions."""
    if not desc:
        return ""

    def _normalize_chunk(chunk: str) -> str:
        s = chunk.strip()
        s = re.sub(r'\[/?gold\]', '', s, flags=re.IGNORECASE)
        s = s.replace('。', '.').replace('！', '!').replace('？', '?')
        s = re.sub(r'\s+', '', s).lower()
        s = s.rstrip('.!?')
        return s

    blocked = {"innate", "exhaust", "ethereal", "固有", "消耗", "虚无"}

    # Remove standalone keyword sentences that appear after another sentence on the same line,
    # e.g. "... Max HP by X. [gold]Exhaust[/gold]."
    keyword_group = r'innate|exhaust|ethereal|固有|消耗|虚无'
    trailing_keyword_sentence = re.compile(
        rf'(?<=[。.!?])\s*\[gold\]?\s*(?:{keyword_group})\s*(?:\[/gold\])?\s*[。.!?]',
        flags=re.IGNORECASE,
    )
    desc = trailing_keyword_sentence.sub('', desc)

    kept = []
    for line in desc.split('\n'):
        chunks = re.findall(r'[^。.!?]+[。.!?]?', line)
        filtered = [chunk for chunk in chunks if _normalize_chunk(chunk) not in blocked]
        merged = ''.join(filtered).strip()
        if merged:
            kept.append(merged)
    return '\n'.join(kept).strip()


def _normalize_zhs_x_spacing(desc: str) -> str:
    """Remove spaces around standalone X tokens in Chinese card text."""
    if not desc:
        return ""
    return re.sub(r'(?<![A-Za-z0-9])\s*X\s*(?![A-Za-z0-9])', 'X', desc)


def _inject_x_plus_one_if_upgraded(desc: str) -> str:
    """Render X as X+1 when upgraded using STS2's IfUpgraded formatter."""
    if not desc:
        return ""
    return re.sub(r'(?<![A-Za-z0-9])X(?![A-Za-z0-9])', 'X{IfUpgraded:show:+1|}', desc)


def _apply_card_specific_desc_fixes(cls_name: str, lang: str, desc: str) -> str:
    """Fix known placeholder mismatches between STS1 text and ClassicMode DynamicVars."""
    def repl_var(old: str, new: str) -> None:
        nonlocal desc
        desc = desc.replace(f"{{{old}:diff()}}", f"{{{new}:diff()}}")
        desc = desc.replace(f"{{{old}}}", f"{{{new}}}")

    def repl_literal(var_name: str, literal: str) -> None:
        nonlocal desc
        desc = desc.replace(f"{{{var_name}:diff()}}", literal)
        desc = desc.replace(f"{{{var_name}}}", literal)

    # Flex / Demon Form use DynamicVar("Strength"), not PowerVar<StrengthPower>.
    if cls_name in {"Flex_C", "DemonForm_C"}:
        repl_var("StrengthPower", "Strength")

    # Perfected Strike uses CalculatedDamage for final hit and ExtraDamage for per-Strike scaling.
    if cls_name == "PerfectedStrike_C":
        repl_var("Damage", "CalculatedDamage")
        repl_var("MagicNumber", "ExtraDamage")
        if "{ExtraDamage:diff()}" not in desc:
            if lang == "zhs":
                desc = re.sub(r"伤害\+\d+", "伤害+{ExtraDamage:diff()}", desc)
            else:
                desc = re.sub(
                    r"Deals?\s+\d+\s+additional\s+damage",
                    "Deals {ExtraDamage:diff()} additional damage",
                    desc,
                    flags=re.IGNORECASE,
                )

    # Blizzard uses ExtraDamage as per-Frost multiplier (not MagicNumber).
    if cls_name == "Blizzard_C":
        repl_var("MagicNumber", "ExtraDamage")

    # Noxious Fumes uses DynamicVar("PoisonPerTurn").
    if cls_name == "NoxiousFumes_C":
        repl_var("PoisonPower", "PoisonPerTurn")

    # Piercing Wail uses DynamicVar("StrLoss").
    if cls_name == "PiercingWail_C":
        repl_var("StrengthPower", "StrLoss")

    # Uppercut / Shockwave use shared DynamicVar("Power") for Weak and Vulnerable amounts.
    if cls_name in {"Uppercut_C", "Shockwave_C"}:
        repl_var("WeakPower", "Power")
        repl_var("VulnerablePower", "Power")

    # Strict placeholder alignment with card CanonicalVars (systematic fixes).
    rename_by_card = {
        "AThousandCuts_C": {"MagicNumber": "CutDamage"},
        "Accuracy_C": {"MagicNumber": "AccuracyPower"},
        "Aggregate_C": {"Cards": "Divisor"},
        "BandageUp_C": {"MagicNumber": "Heal"},
        "BladeDance_C": {"MagicNumber": "Cards"},
        "Blind_C": {"WeakPower": "Weak"},
        "Caltrops_C": {"MagicNumber": "ThornsPower"},
        "ChokeHold_C": {"MagicNumber": "Choke"},
        "Claw_C": {"MagicNumber": "Increase"},
        "CloakAndDagger_C": {"MagicNumber": "Cards"},
        "Concentrate_C": {"Cards": "Discard"},
        "CoreSurge_C": {"MagicNumber": "ArtifactPower"},
        "DarkShackles_C": {"StrengthPower": "StrengthLoss"},
        "Expertise_C": {"Cards": "HandSize", "MagicNumber": "HandSize"},
        "Feed_C": {"MagicNumber": "MaxHp"},
        "FeelNoPain_C": {"MagicNumber": "Power", "Block": "Power"},
        "FlameBarrier_C": {"MagicNumber": "DamageBack"},
        "Ftl_C": {"MagicNumber": "PlayMax", "Cards": "PlayMax"},
        "HandOfGreed_C": {"MagicNumber": "Gold"},
        "JackOfAllTrades_C": {"MagicNumber": "Cards"},
            "Metallicize_C": {"Block": "MagicNumber"},
        "Metamorphosis_C": {"MagicNumber": "Cards"},
        "GeneticAlgorithm_C": {"MagicNumber": "Increase"},
        "Heatsinks_C": {"Cards": "Heatsinks"},
        "HeavyBlade_C": {"Damage": "CalculatedDamage", "MagicNumber": "StrengthMultiplier"},
        "Hemokinesis_C": {"MagicNumber": "HpLoss"},
        "Juggernaut_C": {"MagicNumber": "JuggernautPower"},
        "Lockdown_C": {"MagicNumber": "LockOnPower_C"},
        "Offering_C": {"Cards": "Draw"},
        "Panache_C": {"MagicNumber": "Amount"},
        "Panacea_C": {"MagicNumber": "ArtifactPower"},
        "Rage_C": {"MagicNumber": "RagePower", "Block": "RagePower"},
        "Rampage_C": {"MagicNumber": "Increase"},
        "SadisticNature_C": {"MagicNumber": "Amount"},
        "SelfRepair_C": {"MagicNumber": "Heal"},
        "StaticDischarge_C": {"MagicNumber": "StaticDischarge"},
        "TheBomb_C": {"MagicNumber": "Damage"},
        "Trip_C": {"VulnerablePower": "Vulnerable"},
        "Violence_C": {"MagicNumber": "Cards"},
        "WellLaidPlans_C": {"Cards": "RetainAmount"},
        "WraithForm_C": {"MagicNumber": "IntangiblePower"},
        "Chrysalis_C": {"MagicNumber": "Cards"},
    }
    for old_name, new_name in rename_by_card.get(cls_name, {}).items():
        repl_var(old_name, new_name)

    # Fixed-value cards that do not define matching DynamicVars for these text slots.
    literal_by_card = {
        "BallLightning_C": {"MagicNumber": "1"},
        "Chill_C": {"MagicNumber": "1"},
        "ColdSnap_C": {"MagicNumber": "1"},
        "CompiledDriver_C": {"Cards": "1"},
        "Darkness_C": {"MagicNumber": "1"},
        "DoomAndGloom_C": {"MagicNumber": "1"},
        "Fission_C": {"Cards": "1"},
        "Fusion_C": {"MagicNumber": "1"},
        "Glacier_C": {"MagicNumber": "2"},
        "MeteorStrike_C": {"MagicNumber": "3"},
        "Nightmare_C": {"MagicNumber": "3"},
        "PanicButton_C": {"MagicNumber": "2"},
        "RipAndTear_C": {"MagicNumber": "2"},
        "Streamline_C": {"MagicNumber": "1"},
        "Zap_C": {"MagicNumber": "1"},
    }
    for old_name, literal in literal_by_card.get(cls_name, {}).items():
        repl_literal(old_name, literal)

    return desc


_X_PLUS_ONE_UPGRADE_DESC_CARDS = {
    "Tempest_C",
    "Malaise_C",
    "Doppelganger_C",
    "MultiCast_C",
}


def _find_card_classes(project_dir):
    """Scan ClassicMode .cs files for card class definitions."""
    classes = []
    cards_dir = os.path.join(project_dir, "Cards")
    for cs_file in glob.glob(os.path.join(cards_dir, "**", "*.cs"), recursive=True):
        with open(cs_file, 'r', encoding='utf-8') as f:
            content = f.read()
        for m in re.finditer(r'public sealed class (\w+)\s*:\s*Classic\w*Card', content):
            classes.append(m.group(1))
    return sorted(set(classes))


def _find_power_classes(project_dir):
    """Scan ClassicMode .cs files for power class definitions."""
    classes = []
    powers_dir = os.path.join(project_dir, "Powers")
    for cs_file in glob.glob(os.path.join(powers_dir, "**", "*.cs"), recursive=True):
        with open(cs_file, 'r', encoding='utf-8') as f:
            content = f.read()
        for m in re.finditer(r'public sealed class (\w+)\s*:\s*PowerModel', content):
            classes.append(m.group(1))
    return sorted(set(classes))


def _find_relic_classes(project_dir):
    """Scan ClassicMode .cs files for custom relic class definitions."""
    classes = []
    relics_dir = os.path.join(project_dir, "Relics")
    if not os.path.isdir(relics_dir):
        return classes
    for cs_file in glob.glob(os.path.join(relics_dir, "**", "*.cs"), recursive=True):
        with open(cs_file, 'r', encoding='utf-8') as f:
            content = f.read()
        for m in re.finditer(r'public sealed class (\w+)\s*:\s*ClassicRelic', content):
            classes.append(m.group(1))
    return sorted(set(classes))


def generate_localization(sts1_root, project_dir):
    """Generate STS2-format localization files from STS1 data.

    STS2 mod localization format:
    - Flat dictionary: {"MODEL_ID.title": "Name", "MODEL_ID.description": "Text"}
    - Files placed at ClassicMode/localization/{lang}/ (mod-specific path, NOT localization/)
    - Card vars use :diff() suffix for upgrade display
    """
    langs = ["eng", "zhs"]

    # Clean up old localization files that would override base game
    old_loc_dir = os.path.join(project_dir, "assets", "localization")
    if os.path.isdir(old_loc_dir):
        shutil.rmtree(old_loc_dir)
        print("  Removed old assets/localization/ (was overriding base game)")

    card_loc_by_lang = {}
    relic_loc_by_lang = {}
    power_loc_by_lang = {}
    enchantment_loc_by_lang = {}
    out_dir_by_lang = {}

    for lang in langs:
        cards_src = os.path.join(sts1_root, "localization", lang, "cards.json")

        # Output to mod-specific path: assets/ClassicMode/localization/{lang}/
        out_dir = os.path.join(project_dir, "assets", "ClassicMode", "localization", lang)
        os.makedirs(out_dir, exist_ok=True)

        # --- Cards ---
        if os.path.isfile(cards_src):
            with open(cards_src, 'r', encoding='utf-8') as f:
                sts1_cards = json.load(f)

            card_classes = _find_card_classes(project_dir)

            # Build normalized index of STS1 card IDs
            sts1_index = {}
            for card_id in sts1_cards:
                key = _normalize(card_id)
                if key not in sts1_index:
                    sts1_index[key] = card_id

            card_loc = {}
            unmatched = []

            # Generic fallback prompt used by every card. CardModel.SelectionScreenPrompt
            # throws if the loc key is missing, which silently aborts OnPlay
            # (see DualWield_C). We emit a default for every card so any card that
            # opens a CardSelect screen has at least a passable prompt; cards that
            # need bespoke wording can override via _CARD_PROMPTS below.
            default_prompt = "Choose a card" if lang == "eng" else "\u9009\u62e9\u4e00\u5f20\u724c"

            for cls_name in card_classes:
                if cls_name in _CARD_OVERRIDES:
                    sts1_id = _CARD_OVERRIDES[cls_name]
                    if sts1_id is None:
                        continue
                else:
                    base = cls_name[:-2] if cls_name.endswith("_C") else cls_name
                    sts1_id = sts1_index.get(_normalize(base))

                if sts1_id and sts1_id in sts1_cards:
                    entry = sts1_cards[sts1_id]
                    model_key = _slugify(cls_name)
                    card_loc[f"{model_key}.title"] = entry["NAME"]
                    desc = _convert_card_desc(entry.get("DESCRIPTION", ""))
                    # Cards using RepeatVar need {MagicNumber} → {Repeat} so the
                    # SmartFormat lookup against DynamicVarSet finds the value.
                    if cls_name in _REPEAT_VAR_CARDS:
                        desc = desc.replace("{MagicNumber:", "{Repeat:")
                        desc = desc.replace("{MagicNumber}", "{Repeat}")
                    desc = _apply_card_specific_desc_fixes(cls_name, lang, desc)
                    if cls_name in _X_PLUS_ONE_UPGRADE_DESC_CARDS:
                        desc = _inject_x_plus_one_if_upgraded(desc)
                    if lang == "zhs":
                        desc = _normalize_zhs_x_spacing(desc)
                    # STS2 renders keyword tags (e.g. Exhaust/Innate/Ethereal) from card metadata.
                    # Remove standalone keyword sentences copied from STS1 descriptions to avoid duplicates.
                    desc = _strip_auto_keyword_sentences(desc)
                    card_loc[f"{model_key}.description"] = desc
                    card_loc[f"{model_key}.selectionScreenPrompt"] = default_prompt
                else:
                    unmatched.append(cls_name)

            if unmatched:
                print(f"  WARNING ({lang}): {len(unmatched)} unmatched cards: {', '.join(unmatched)}")

            custom_card_src = _CUSTOM_CARD_LOC.get(lang, {})
            for class_name, entry in custom_card_src.items():
                slug = _slugify(class_name)
                card_loc[f"{slug}.title"] = entry["NAME"]
                card_loc[f"{slug}.description"] = entry["DESCRIPTION"]
                card_loc[f"{slug}.selectionScreenPrompt"] = default_prompt

            # Bespoke per-card prompts (overrides default).
            prompt_overrides = _CARD_PROMPTS.get(lang, {})
            for class_name, prompt in prompt_overrides.items():
                slug = _slugify(class_name)
                card_loc[f"{slug}.selectionScreenPrompt"] = prompt

            # Keep non-card-model custom keys that are consumed by runtime patches.
            card_loc.update(_EXTRA_CARD_LOC.get(lang, {}))

            card_loc_by_lang[lang] = card_loc
            out_dir_by_lang[lang] = out_dir

        # --- Relics ---
        relic_loc = {}
        relic_classes = _find_relic_classes(project_dir)
        relics_src = os.path.join(sts1_root, "localization", lang, "relics.json")
        sts1_relics = {}
        if os.path.isfile(relics_src):
            with open(relics_src, 'r', encoding='utf-8') as f:
                sts1_relics = json.load(f)

        if sts1_relics:
            sts1_index = {}
            for relic_id in sts1_relics:
                key = _normalize(relic_id)
                if key not in sts1_index:
                    sts1_index[key] = relic_id

            unmatched_relics = []
            for cls_name in relic_classes:
                override_id = _CUSTOM_RELIC_STS1_ID_OVERRIDES.get(cls_name)
                if override_id is None:
                    base = cls_name
                    if base.endswith("Relic"):
                        base = base[:-5]
                    if base.startswith("Classic") and len(base) > len("Classic"):
                        base = base[len("Classic"):]
                    sts1_id = sts1_index.get(_normalize(base))
                else:
                    sts1_id = override_id

                if sts1_id and sts1_id in sts1_relics:
                    entry = sts1_relics[sts1_id]
                    raw_desc = entry.get("DESCRIPTION", "")
                    if not raw_desc:
                        desc_parts = entry.get("DESCRIPTIONS", [])
                        if isinstance(desc_parts, list):
                            raw_desc = "".join(str(p) for p in desc_parts)
                    slug = _slugify(cls_name)
                    relic_loc[f"{slug}.title"] = entry.get("NAME", cls_name)
                    relic_loc[f"{slug}.description"] = _convert_sts1_markup_to_sts2(raw_desc)
                    relic_loc[f"{slug}.flavor"] = entry.get("FLAVOR", "")
                else:
                    unmatched_relics.append(cls_name)

            if unmatched_relics:
                print(f"  WARNING ({lang}): {len(unmatched_relics)} unmatched relics: {', '.join(unmatched_relics)}")

        # Fallback/override entries (keeps bespoke custom wording if explicitly provided)
        relic_src = _CUSTOM_RELIC_LOC.get(lang, {})
        for class_name, entry in relic_src.items():
            slug = _slugify(class_name)
            title_key = f"{slug}.title"
            desc_key = f"{slug}.description"
            flavor_key = f"{slug}.flavor"
            if class_name in _FORCE_CUSTOM_RELIC_LOCALIZATION or title_key not in relic_loc:
                relic_loc[title_key] = entry["NAME"]
            if class_name in _FORCE_CUSTOM_RELIC_LOCALIZATION or desc_key not in relic_loc:
                relic_loc[desc_key] = entry["DESCRIPTION"]
            if class_name in _FORCE_CUSTOM_RELIC_LOCALIZATION or flavor_key not in relic_loc:
                relic_loc[flavor_key] = entry.get("FLAVOR", "")

        if relic_loc:
            for source_slug, alias_slug in _RELIC_LOC_KEY_ALIASES.items():
                for suffix in ("title", "description", "flavor"):
                    source_key = f"{source_slug}.{suffix}"
                    alias_key = f"{alias_slug}.{suffix}"
                    if source_key in relic_loc and alias_key not in relic_loc:
                        relic_loc[alias_key] = relic_loc[source_key]
            relic_loc_by_lang[lang] = relic_loc

        # --- Powers ---
        power_src = _CUSTOM_POWER_LOC.get(lang, {})
        if power_src:
            power_loc = {}
            for class_name, entry in power_src.items():
                slug = _slugify(class_name)
                desc = _convert_sts1_markup_to_sts2(entry["DESCRIPTION"])
                desc = _normalize_power_wording(desc, lang, slug)
                power_loc[f"{slug}.title"] = entry["NAME"]
                power_loc[f"{slug}.description"] = desc
                # Power hover text only injects {Amount} and dynamic vars through
                # smartDescription path for mutable instances.
                power_loc[f"{slug}.smartDescription"] = desc
            power_loc_by_lang[lang] = power_loc

        # --- Enchantments ---
        enchantment_src = _CUSTOM_ENCHANTMENT_LOC.get(lang, {})
        if enchantment_src:
            enchantment_loc = {}
            for class_name, entry in enchantment_src.items():
                slug = _slugify(class_name)
                enchantment_loc[f"{slug}.title"] = entry["NAME"]
                enchantment_loc[f"{slug}.description"] = entry["DESCRIPTION"]
            enchantment_loc_by_lang[lang] = enchantment_loc

    # Run zh cleanup/highlight rules after both language card dictionaries are built.
    for lang in langs:
        if lang in card_loc_by_lang:
            _normalize_placeholder_case_in_loc_dict(card_loc_by_lang[lang], f"cards/{lang}")
        if lang in relic_loc_by_lang:
            _normalize_placeholder_case_in_loc_dict(relic_loc_by_lang[lang], f"relics/{lang}")
        if lang in power_loc_by_lang:
            _normalize_placeholder_case_in_loc_dict(power_loc_by_lang[lang], f"powers/{lang}")
            _normalize_power_amount_diff(power_loc_by_lang[lang], f"powers/{lang}")
        if lang in enchantment_loc_by_lang:
            _normalize_placeholder_case_in_loc_dict(enchantment_loc_by_lang[lang], f"enchantments/{lang}")

    # Run zh cleanup/highlight rules after both language dictionaries are built.
    if "eng" in card_loc_by_lang and "zhs" in card_loc_by_lang:
        _apply_zh_spacing_and_highlight(card_loc_by_lang["zhs"], card_loc_by_lang["eng"])
    if "eng" in relic_loc_by_lang and "zhs" in relic_loc_by_lang:
        _apply_zh_term_highlight_to_loc_dict(relic_loc_by_lang["zhs"], relic_loc_by_lang["eng"], "relics")
    if "eng" in power_loc_by_lang and "zhs" in power_loc_by_lang:
        _apply_zh_term_highlight_to_loc_dict(power_loc_by_lang["zhs"], power_loc_by_lang["eng"], "powers")
    if "eng" in enchantment_loc_by_lang and "zhs" in enchantment_loc_by_lang:
        _apply_zh_term_highlight_to_loc_dict(enchantment_loc_by_lang["zhs"], enchantment_loc_by_lang["eng"], "enchantments")

    # Write cards.json for each language.
    for lang in langs:
        if lang not in card_loc_by_lang:
            continue
        out_dir = out_dir_by_lang[lang]
        with open(os.path.join(out_dir, "cards.json"), 'w', encoding='utf-8') as f:
            json.dump(card_loc_by_lang[lang], f, ensure_ascii=False, indent=2)
        print(f"  Generated {lang}/cards.json: {len(card_loc_by_lang[lang]) // 2} entries")

        if lang in relic_loc_by_lang:
            with open(os.path.join(out_dir, "relics.json"), 'w', encoding='utf-8') as f:
                json.dump(relic_loc_by_lang[lang], f, ensure_ascii=False, indent=2)
            print(f"  Generated {lang}/relics.json: {len(relic_loc_by_lang[lang]) // 2} entries")

        if lang in power_loc_by_lang:
            with open(os.path.join(out_dir, "powers.json"), 'w', encoding='utf-8') as f:
                json.dump(power_loc_by_lang[lang], f, ensure_ascii=False, indent=2)
            print(f"  Generated {lang}/powers.json: {len(power_loc_by_lang[lang]) // 2} entries")

        if lang in enchantment_loc_by_lang:
            with open(os.path.join(out_dir, "enchantments.json"), 'w', encoding='utf-8') as f:
                json.dump(enchantment_loc_by_lang[lang], f, ensure_ascii=False, indent=2)
            print(f"  Generated {lang}/enchantments.json: {len(enchantment_loc_by_lang[lang]) // 2} entries")


def main():
    if len(sys.argv) < 3:
        print(f"Usage: {sys.argv[0]} <project_dir> <pck_root> [<sts1_unpacked_dir>]")
        sys.exit(1)

    project_dir = sys.argv[1]
    pck_root = sys.argv[2]

    # STS1 unpacked assets root:
    #   1. explicit 3rd CLI argument
    #   2. STS1_UNPACKED_DIR environment variable
    #   3. legacy fallback: sibling "SlayTheSpire_unpacked" next to the repo
    if len(sys.argv) >= 4 and sys.argv[3]:
        sts1_root = sys.argv[3]
    elif os.environ.get("STS1_UNPACKED_DIR"):
        sts1_root = os.environ["STS1_UNPACKED_DIR"]
    else:
        repo_root = os.path.normpath(os.path.join(project_dir, "..", ".."))
        sts1_root = os.path.join(repo_root, "SlayTheSpire_unpacked")

    if not os.path.isdir(sts1_root):
        print(f"ERROR: STS1 unpacked directory not found: {sts1_root}")
        print("  Provide it via:")
        print("    python prepare_assets.py <project_dir> <pck_root> <sts1_unpacked_dir>")
        print("  or set STS1_UNPACKED_DIR in your environment.")
        print("  See README.md for how to unpack Slay the Spire 1 assets.")
        sys.exit(1)

    print("[ClassicMode] Preparing assets...")

    # 1. Generate localization from STS1 data
    print("  Generating localization...")
    generate_localization(sts1_root, project_dir)

    # 2. Copy localization + other assets to PCK
    print("  Copying localization files...")
    copy_localization(project_dir, pck_root)

    # 3. Copy STS1 card portraits
    print("  Copying STS1 card portraits...")
    copy_card_portraits(sts1_root, project_dir, pck_root)

    # 4. Copy STS1 relic images
    print("  Copying STS1 relic images...")
    copy_relic_images(sts1_root, pck_root)

    # 4.5 Create custom enchantment icons from cropped relic icons
    print("  Preparing custom enchantment icons...")
    copy_custom_enchantment_icons(pck_root)

    # 5. Copy selected STS1 power icons (atlas slices) for classic powers
    print("  Importing STS1 power icons...")
    copy_power_icons_from_sts1(sts1_root, pck_root)

    print("[ClassicMode] Asset preparation complete.")


if __name__ == "__main__":
    main()
