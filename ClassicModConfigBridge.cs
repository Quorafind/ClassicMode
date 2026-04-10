using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace ClassicModeMod;

internal static class ClassicModConfigBridge
{
    private const string ModId = "ClassicMode";
    private static bool _available;
    private static bool _registered;
    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    internal static bool IsAvailable => _available;

    internal static void DeferredRegister()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
        {
            tree.ProcessFrame += OnNextFrame;
        }
    }

    private static void OnNextFrame()
    {
        if (Engine.GetMainLoop() is SceneTree tree)
        {
            tree.ProcessFrame -= OnNextFrame;
        }

        Detect();
        if (_available)
        {
            Register();
        }
    }

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch
                {
                    return Type.EmptyTypes;
                }
            }).ToArray();

            _apiType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigType");
            _available = _apiType != null && _entryType != null && _configTypeEnum != null;
        }
        catch (Exception ex)
        {
            Log.Warn($"[ClassicMode] ModConfig detect failed: {ex.Message}");
            _available = false;
        }
    }

    private static void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        try
        {
            var entries = BuildEntries();
            var labels = new Dictionary<string, string>
            {
                ["en"] = "Classic Mode",
                ["zhs"] = "经典模式"
            };

            var registerMethod = _apiType!
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.Name == "Register")
                .OrderByDescending(m => m.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(null, new object[] { ModId, labels["en"], labels, entries });
            }
            else
            {
                registerMethod.Invoke(null, new object[] { ModId, labels["en"], entries });
            }

            SetValue("markClassicCardOrigin", ClassicConfig.MarkClassicCardOrigin);
            Log.Info("[ClassicMode] Registered ModConfig settings.");
        }
        catch (Exception ex)
        {
            Log.Warn($"[ClassicMode] ModConfig register failed: {ex.Message}");
        }
    }

    internal static void SetValue(string key, object value)
    {
        if (!_available)
        {
            return;
        }

        try
        {
            _apiType?.GetMethod("SetValue", BindingFlags.Static | BindingFlags.Public)
                ?.Invoke(null, new object[] { ModId, key, value });
        }
        catch
        {
        }
    }

    private static Array BuildEntries()
    {
        var entries = new List<object>();

        entries.Add(Entry(cfg =>
        {
            Set(cfg, "Label", "Card Marking");
            Set(cfg, "Labels", L("Card Marking", "卡牌标记"));
            Set(cfg, "Type", EnumVal("Header"));
        }));

        entries.Add(Entry(cfg =>
        {
            Set(cfg, "Key", "markClassicCardOrigin");
            Set(cfg, "Label", "Mark STS1 Cards");
            Set(cfg, "Labels", L("Mark STS1 Cards", "标记一代卡牌"));
            Set(cfg, "Type", EnumVal("Toggle"));
            Set(cfg, "DefaultValue", false);
            Set(cfg, "Description", "Show an extra tip on classic cards.");
            Set(cfg, "Descriptions", L("Show an extra tip on classic cards.", "在经典卡上显示额外来源提示。"));
            Set(cfg, "OnChanged", (Action<object>)(v =>
            {
                ClassicConfig.MarkClassicCardOrigin = Convert.ToBoolean(v);
            }));
        }));

        var array = Array.CreateInstance(_entryType!, entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            array.SetValue(entries[i], i);
        }

        return array;
    }

    private static object Entry(Action<object> init)
    {
        var cfg = Activator.CreateInstance(_entryType!)!;
        init(cfg);
        return cfg;
    }

    private static object EnumVal(string name)
    {
        return Enum.Parse(_configTypeEnum!, name, ignoreCase: true);
    }

    private static Dictionary<string, string> L(string en, string zhs)
    {
        return new Dictionary<string, string>
        {
            ["en"] = en,
            ["zhs"] = zhs
        };
    }

    private static void Set(object target, string property, object value)
    {
        var p = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
        p?.SetValue(target, value);
    }
}
