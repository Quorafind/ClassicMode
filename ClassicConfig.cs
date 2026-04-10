using System.IO;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace ClassicModeMod;

public static class ClassicConfig
{
    private static readonly string ConfigPath = Path.Combine(
        Path.GetDirectoryName(typeof(ClassicConfig).Assembly.Location)!,
        "classic_config.json");

    private static bool _classicCards;
    private static bool _classicRelics;
    private static bool _classicHybrid;
    private static bool _hybridDedupe;
    private static bool _markClassicCardOrigin;

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

    public static bool ClassicRelics
    {
        get => _classicRelics;
        set
        {
            if (_classicRelics == value) return;
            _classicRelics = value;
            HybridPoolCache.InvalidateAll();
            Save();
        }
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

    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var data = JsonSerializer.Deserialize<ConfigData>(json);
                if (data != null)
                {
                    _classicCards = data.ClassicCards;
                    _classicRelics = data.ClassicRelics;
                    _classicHybrid = data.ClassicHybrid;
                    _hybridDedupe = data.HybridDedupe;
                    _markClassicCardOrigin = data.MarkClassicCardOrigin;
                }
                Log.Info($"[ClassicMode] Config loaded: Cards={_classicCards}, Relics={_classicRelics}, Hybrid={_classicHybrid}, Dedupe={_hybridDedupe}, MarkSTS1={_markClassicCardOrigin}");
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
                ClassicRelics = _classicRelics,
                ClassicHybrid = _classicHybrid,
                HybridDedupe = _hybridDedupe,
                MarkClassicCardOrigin = _markClassicCardOrigin
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
        public bool ClassicRelics { get; set; }
        public bool ClassicHybrid { get; set; }
        public bool HybridDedupe { get; set; }
        public bool MarkClassicCardOrigin { get; set; }
    }
}
