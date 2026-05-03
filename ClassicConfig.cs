using System.IO;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace ClassicModeMod;

public static class ClassicConfig
{
    private static readonly string ConfigPath = Path.Combine(
        Path.GetDirectoryName(typeof(ClassicConfig).Assembly.Location)!,
        "classic_config.cfg");

    private static readonly string LegacyConfigPath = Path.Combine(
        Path.GetDirectoryName(typeof(ClassicConfig).Assembly.Location)!,
        "classic_config.json");

    private static bool _classicCards;
    private static bool _addClassicRelics;
    private static bool _onlyClassicRelics;
    private static bool _classicHybrid;
    private static bool _hybridDedupe;
    private static bool _classicColorless;
    private static bool _classicColorlessHybrid;
    private static bool _classicColorlessDedupe;
    private static bool _colorlessCardRewards;
    private static bool _markClassicCardOrigin;
    private static bool _replaceAncientsWithDarv;

    // Every pool-affecting toggle invalidates ModelDb's lazy `_allCards` /
    // `_allCardPools` / `_allCharacterCardPools` / relic equivalents on
    // change. Without this, the encyclopedia and any other consumer of
    // `ModelDb.AllCards` keeps showing whatever pool was active at game boot.
    public static bool ClassicCards
    {
        get => _classicCards;
        set
        {
            if (_classicCards == value) return;
            _classicCards = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    public static bool AddClassicRelics
    {
        get => _addClassicRelics;
        set
        {
            if (_addClassicRelics == value) return;
            _addClassicRelics = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    public static bool OnlyClassicRelics
    {
        get => _onlyClassicRelics;
        set
        {
            if (_onlyClassicRelics == value) return;
            _onlyClassicRelics = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    // Backward-compat alias for old code paths.
    public static bool ClassicRelics
    {
        get => OnlyClassicRelics;
        set => OnlyClassicRelics = value;
    }

    /// <summary>
    /// When true, card and relic pools merge STS1 (classic) + STS2 entries.
    /// Takes precedence over ClassicCards / ClassicRelics when enabled.
    /// </summary>
    public static bool ClassicHybrid
    {
        get => _classicHybrid;
        set
        {
            if (_classicHybrid == value) return;
            _classicHybrid = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    /// <summary>
    /// When true (and Hybrid is on), overlapping cards / relics that share
    /// a display name are deduped: STS2 versions win, STS1 duplicates are dropped.
    /// </summary>
    public static bool HybridDedupe
    {
        get => _hybridDedupe;
        set
        {
            if (_hybridDedupe == value) return;
            _hybridDedupe = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    /// <summary>
    /// When true, use STS1 classic colorless cards as the colorless pool.
    /// </summary>
    public static bool ClassicColorless
    {
        get => _classicColorless;
        set
        {
            if (_classicColorless == value) return;
            _classicColorless = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    /// <summary>
    /// When true, merge STS1 classic colorless cards into the base STS2 colorless pool.
    /// Mutually exclusive with ClassicColorless.
    /// </summary>
    public static bool ClassicColorlessHybrid
    {
        get => _classicColorlessHybrid;
        set
        {
            if (_classicColorlessHybrid == value) return;
            _classicColorlessHybrid = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    /// <summary>
    /// When true (and ClassicColorlessHybrid is on), remove same-name duplicates so base STS2 cards win.
    /// </summary>
    public static bool ClassicColorlessDedupe
    {
        get => _classicColorlessDedupe;
        set
        {
            if (_classicColorlessDedupe == value) return;
            _classicColorlessDedupe = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
    }

    /// <summary>
    /// When true, card rewards may include colorless cards.
    /// Shops are unaffected (they already have colorless slots).
    /// </summary>
    public static bool ColorlessCardRewards
    {
        get => _colorlessCardRewards;
        set
        {
            if (_colorlessCardRewards == value) return;
            _colorlessCardRewards = value;
            Save();
        }
    }

    /// <summary>
    /// When true, classic (STS1) cards show an origin hover tip to make
    /// them visually distinguishable in mixed pools.
    /// </summary>
    public static bool MarkClassicCardOrigin
    {
        get => _markClassicCardOrigin;
        set
        {
            if (_markClassicCardOrigin == value) return;
            _markClassicCardOrigin = value;
            Save();
        }
    }

    /// <summary>
    /// When true, force Act2/Act3 ancient encounters to use Darv and replace
    /// Darv options with STS1 boss-relic picks.
    /// </summary>
    public static bool ReplaceAncientsWithDarv
    {
        get => _replaceAncientsWithDarv;
        set
        {
            if (_replaceAncientsWithDarv == value) return;
            _replaceAncientsWithDarv = value;
            Save();
        }
    }

    public static void Load()
    {
        try
        {
            var resolvedPath = File.Exists(ConfigPath) ? ConfigPath : LegacyConfigPath;
            if (File.Exists(resolvedPath))
            {
                var json = File.ReadAllText(resolvedPath);
                var data = JsonSerializer.Deserialize<ConfigData>(json);
                if (data != null)
                {
                    _classicCards = data.ClassicCards;
                    _addClassicRelics = data.AddClassicRelics;
                    _onlyClassicRelics = data.OnlyClassicRelics || data.ClassicRelics;
                    _classicHybrid = data.ClassicHybrid;
                    _hybridDedupe = data.HybridDedupe;
                    _classicColorless = data.ClassicColorless;
                    _classicColorlessHybrid = data.ClassicColorlessHybrid;
                    _classicColorlessDedupe = data.ClassicColorlessDedupe;
                    _colorlessCardRewards = data.ColorlessCardRewards;
                    _markClassicCardOrigin = data.MarkClassicCardOrigin;
                    _replaceAncientsWithDarv = data.ReplaceAncientsWithDarv;
                }
                Log.Info($"[ClassicMode] Config loaded: Cards={_classicCards}, AddRelics={_addClassicRelics}, OnlyRelics={_onlyClassicRelics}, HybridCards={_classicHybrid}, Dedupe={_hybridDedupe}, ColorlessClassic={_classicColorless}, ColorlessHybrid={_classicColorlessHybrid}, ColorlessDedupe={_classicColorlessDedupe}, ColorlessRewards={_colorlessCardRewards}, MarkSTS1={_markClassicCardOrigin}");

                // Migrate legacy filename so ModManager doesn't mistake it for a manifest.
                if (resolvedPath == LegacyConfigPath)
                {
                    Save();
                    try
                    {
                        File.Delete(LegacyConfigPath);
                    }
                    catch
                    {
                        // Non-fatal; keep running even if old file cannot be removed.
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[ClassicMode] Failed to load config: {ex.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            var data = new ConfigData
            {
                ClassicCards = _classicCards,
                AddClassicRelics = _addClassicRelics,
                OnlyClassicRelics = _onlyClassicRelics,
                ClassicRelics = _onlyClassicRelics,
                ClassicHybrid = _classicHybrid,
                HybridDedupe = _hybridDedupe,
                ClassicColorless = _classicColorless,
                ClassicColorlessHybrid = _classicColorlessHybrid,
                ClassicColorlessDedupe = _classicColorlessDedupe,
                ColorlessCardRewards = _colorlessCardRewards,
                MarkClassicCardOrigin = _markClassicCardOrigin,
                ReplaceAncientsWithDarv = _replaceAncientsWithDarv
            };
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Error($"[ClassicMode] Failed to save config: {ex.Message}");
        }
    }

    private class ConfigData
    {
        public bool ClassicCards { get; set; }
        public bool AddClassicRelics { get; set; }
        public bool OnlyClassicRelics { get; set; }
        // Legacy field for backward compatibility.
        public bool ClassicRelics { get; set; }
        public bool ClassicHybrid { get; set; }
        public bool HybridDedupe { get; set; }
        public bool ClassicColorless { get; set; }
        public bool ClassicColorlessHybrid { get; set; }
        public bool ClassicColorlessDedupe { get; set; }
        public bool ColorlessCardRewards { get; set; }
        public bool MarkClassicCardOrigin { get; set; }
        public bool ReplaceAncientsWithDarv { get; set; }
    }
}
