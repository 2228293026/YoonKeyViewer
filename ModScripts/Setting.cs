using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace YoonKeyViewer
{
    /// <summary>
    /// Per-character configuration. Each character (Yoon/Line/Delebi) gets its own instance.
    /// Yoon-specific fields (NervousBPM, HideFeet, HideFeetKeyboard, FKeyCodes) are included
    /// here for simplicity; they are only read when Character == Yoon.
    /// </summary>
    public class PerCharacterConfig
    {
        public float Size = 1f;
        public float LocationX = 0f;
        public float LocationY = 1f;
        public bool FlipHorizontal = false;
        public bool HideDesk = false;

        // Yoon-only fields
        public float NervousBPM = 300f;
        public bool HideFeet = false;
        public bool HideFeetKeyboard = false;

        public KeyCode[] KeyCodes;
        public KeyCode[] FKeyCodes = { KeyCode.F2, KeyCode.F3, KeyCode.F7, KeyCode.F8 };
    }

    public class Setting : UnityModManager.ModSettings
    {
        public CharacterType Character = CharacterType.Yoon;
        public PerCharacterConfig YoonConfig;
        public PerCharacterConfig LineConfig;
        public PerCharacterConfig DelebiConfig;

        public bool ShareJipperResourcePack = true;

        [JsonIgnore]
        public bool KeyCodeJipperResourcePack = false;

        /// <summary>Returns the config for the currently selected character.</summary>
        [JsonIgnore]
        public PerCharacterConfig Current => Character switch
        {
            CharacterType.Yoon => YoonConfig,
            CharacterType.Line => LineConfig,
            CharacterType.Delebi => DelebiConfig,
            _ => YoonConfig,
        };

        public readonly Dictionary<string, Dictionary<SystemLanguage, string>> Localization = new()
        {
            {
                "ykv.size", new()
                {
                    { SystemLanguage.English, "Size" },
                    { SystemLanguage.German, "Größe" },
                    { SystemLanguage.Korean, "크기" },
                    { SystemLanguage.ChineseSimplified, "大小" }
                }
            },
            {
                "ykv.posx", new()
                {
                    { SystemLanguage.English, "X Location" },
                    { SystemLanguage.German, "X-Position" },
                    { SystemLanguage.Korean, "X 위치" },
                    { SystemLanguage.ChineseSimplified, "X 位置" }
                }
            },
            {
                "ykv.posy", new()
                {
                    { SystemLanguage.English, "Y Location" },
                    { SystemLanguage.German, "Y-Position" },
                    { SystemLanguage.Korean, "Y 위치" },
                    { SystemLanguage.ChineseSimplified, "Y 位置" }
                }
            },
            {
                "ykv.nervousbpm", new()
                {
                    { SystemLanguage.English, "Yoon is nervous if current BPM exceeds" },
                    { SystemLanguage.German, "Yoon wird nervös, wenn dieses BPM übersteigt" },
                    { SystemLanguage.Korean, "굥욷이 힘들어하는 체감 BPM 값" },
                    { SystemLanguage.ChineseSimplified, "当前 BPM 超过此值时 Yoon 会紧张" }
                }
            },
            {
                "ykv.fliphorizontal", new()
                {
                    { SystemLanguage.English, "Flip Horizontal" },
                    { SystemLanguage.German, "Horizontal spiegeln" },
                    { SystemLanguage.Korean, "수평 반전" },
                    { SystemLanguage.ChineseSimplified, "水平翻转" }
                }
            },
            {
                "ykv.hidedesk", new()
                {
                    { SystemLanguage.English, "Hide Desk" },
                    { SystemLanguage.German, "Schreibtisch ausblenden" },
                    { SystemLanguage.Korean, "책상 숨기기" },
                    { SystemLanguage.ChineseSimplified, "隐藏桌子" }
                }
            },
            {
                "ykv.pressakey", new()
                {
                    { SystemLanguage.English, "Press a key" },
                    { SystemLanguage.German, "Drücken Sie eine Taste" },
                    { SystemLanguage.Korean, "키를 누르세요" },
                    { SystemLanguage.ChineseSimplified, "按一个键" }
                }
            },
            {
                "ykv.settingslabel", new()
                {
                    { SystemLanguage.English, "Key Viewer Settings" },
                    { SystemLanguage.German, "Tastenanzeige-Einstellungen" },
                    { SystemLanguage.Korean, "키뷰어 설정" },
                    { SystemLanguage.ChineseSimplified, "按键查看器设置" }
                }
            },
            {
                "ykv.hidelegs", new()
                {
                    { SystemLanguage.English, "Hide Legs" },
                    { SystemLanguage.German, "Beine ausblenden" },
                    { SystemLanguage.Korean, "다리 숨기기" },
                    { SystemLanguage.ChineseSimplified, "隐藏腿" }
                }
            },
            {
                "ykv.hidefeetkeyboard", new()
                {
                    { SystemLanguage.English, "Hide Feet Keyboard" },
                    { SystemLanguage.German, "Fußtastatur ausblenden" },
                    { SystemLanguage.Korean, "발 키보드 숨기기" },
                    { SystemLanguage.ChineseSimplified, "隐藏脚用键盘" }
                }
            },
            {
                "ykv.toprow", new()
                {
                    { SystemLanguage.English, "Top Row" },
                    { SystemLanguage.German, "Obere Reihe" },
                    { SystemLanguage.Korean, "상단 줄" },
                    { SystemLanguage.ChineseSimplified, "上排" }
                }
            },
            {
                "ykv.bottomrow", new()
                {
                    { SystemLanguage.English, "Bottom Row" },
                    { SystemLanguage.German, "Untere Reihe" },
                    { SystemLanguage.Korean, "하단 줄" },
                    { SystemLanguage.ChineseSimplified, "下排" }
                }
            },
            {
                "ykv.feetkeyboard", new()
                {
                    { SystemLanguage.English, "Feet Keyboard" },
                    { SystemLanguage.German, "Fußtastatur" },
                    { SystemLanguage.Korean, "발 키보드" },
                    { SystemLanguage.ChineseSimplified, "脚用键盘" }
                }
            },
            {
                "ykv.character", new()
                {
                    { SystemLanguage.English, "Character" },
                    { SystemLanguage.German, "Charakter" },
                    { SystemLanguage.Korean, "캐릭터" },
                    { SystemLanguage.ChineseSimplified, "角色" }
                }
            },
            {
                "ykv.char.delebi", new()
                {
                    { SystemLanguage.English, "Delebi" },
                    { SystemLanguage.Korean, "델레비" },
                    { SystemLanguage.ChineseSimplified, "Delebi" }
                }
            }
        };

        // ── Factory: default config per character ──

        private static PerCharacterConfig DefaultYoonConfig() => new()
        {
            KeyCodes = new KeyCode[]
            {
                KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
                KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon,
                KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
                KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period
            },
            FKeyCodes = new KeyCode[]
            {
                KeyCode.F2, KeyCode.F3, KeyCode.F7, KeyCode.F8
            },
        };

        private static PerCharacterConfig DefaultLineConfig() => new()
        {
            KeyCodes = new KeyCode[]
            {
                KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
                KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon,
                KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V,
                KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period
            },
        };

        private static PerCharacterConfig DefaultDelebiConfig() => new()
        {
            KeyCodes = new KeyCode[]
            {
                KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F,
                KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon,
                KeyCode.Z, KeyCode.X
            },
        };

        public string GetLocalized(string key)
        {
            if (Localization.TryGetValue(key, out var langDict) &&
                langDict.TryGetValue(RDString.language, out var localizedText))
            {
                return localizedText;
            }

            return langDict?.TryGetValue(SystemLanguage.English, out var fallback) == true ? fallback : key;
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            try
            {
                var filepath = GetPath(modEntry);
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                var json = JsonConvert.SerializeObject(this, settings);
                File.WriteAllText(filepath, json);
            }
            catch (Exception e)
            {
                Main.Logger?.Log($"[Setting.Save] Exception: {e}");
            }
        }

        public override string GetPath(UnityModManager.ModEntry modEntry)
        {
            return Path.Combine(modEntry.Path, GetType().Name + ".json");
        }

        public static Setting Load(UnityModManager.ModEntry modEntry)
        {
            var filepath = Path.Combine(modEntry.Path, typeof(Setting).Name + ".json");

            if (!File.Exists(filepath))
            {
                var fresh = new Setting();
                fresh.YoonConfig = DefaultYoonConfig();
                fresh.LineConfig = DefaultLineConfig();
                fresh.DelebiConfig = DefaultDelebiConfig();
                return fresh;
            }

            try
            {
                var json = File.ReadAllText(filepath);

                // ── Migration: detect old flat-field format ──
                var obj = JObject.Parse(json);
                if (obj["YoonConfig"] == null || obj["YoonConfig"].Type == JTokenType.Null)
                {
                    return MigrateFromV1(json, filepath, modEntry);
                }

                var serializerSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ObjectCreationHandling = ObjectCreationHandling.Replace
                };

                var setting = JsonConvert.DeserializeObject<Setting>(json, serializerSettings) ?? new Setting();

                // Ensure configs are never null
                setting.YoonConfig ??= DefaultYoonConfig();
                setting.LineConfig ??= DefaultLineConfig();
                setting.DelebiConfig ??= DefaultDelebiConfig();

                // Ensure KeyCodes arrays are never null
                if (setting.YoonConfig.KeyCodes == null || setting.YoonConfig.KeyCodes.Length == 0)
                    setting.YoonConfig.KeyCodes = DefaultYoonConfig().KeyCodes;
                if (setting.LineConfig.KeyCodes == null || setting.LineConfig.KeyCodes.Length == 0)
                    setting.LineConfig.KeyCodes = DefaultLineConfig().KeyCodes;
                if (setting.DelebiConfig.KeyCodes == null || setting.DelebiConfig.KeyCodes.Length == 0)
                    setting.DelebiConfig.KeyCodes = DefaultDelebiConfig().KeyCodes;
                if (setting.YoonConfig.FKeyCodes == null || setting.YoonConfig.FKeyCodes.Length == 0)
                    setting.YoonConfig.FKeyCodes = DefaultYoonConfig().FKeyCodes;

                return setting;
            }
            catch (Exception e)
            {
                Main.Logger?.Log($"[Setting.Load] Failed to load settings: {e.Message}");
                var fallback = new Setting();
                fallback.YoonConfig = DefaultYoonConfig();
                fallback.LineConfig = DefaultLineConfig();
                fallback.DelebiConfig = DefaultDelebiConfig();
                return fallback;
            }
        }

        /// <summary>Migrate from v1 flat-field format to v2 per-character format.</summary>
        private static Setting MigrateFromV1(string json, string filepath, UnityModManager.ModEntry modEntry)
        {
            Main.Logger?.Log("[Setting.Load] Detected v1 config format — migrating to v2 per-character format.");

            try
            {
                var old = JsonConvert.DeserializeObject<SettingV1>(json);
                if (old == null) throw new Exception("Failed to deserialize v1 config");

                var setting = new Setting();
                setting.Character = old.Character;

                // Yoon gets the old flat values (most likely what the user was using)
                setting.YoonConfig = new PerCharacterConfig
                {
                    Size = old.Size,
                    LocationX = old.LocationX,
                    LocationY = old.LocationY,
                    FlipHorizontal = old.FlipHorizontal,
                    HideDesk = old.HideDesk,
                    HideFeet = old.HideFeet,
                    HideFeetKeyboard = old.HideFeetKeyboard,
                    NervousBPM = old.NervousBPM,
                    KeyCodes = old.YoonKeyCodes ?? DefaultYoonConfig().KeyCodes,
                    FKeyCodes = old.FKeyCodes ?? DefaultYoonConfig().FKeyCodes,
                };

                setting.LineConfig = new PerCharacterConfig
                {
                    KeyCodes = old.LineKeyCodes ?? DefaultLineConfig().KeyCodes,
                };

                setting.DelebiConfig = new PerCharacterConfig
                {
                    KeyCodes = old.DelebiKeyCodes ?? DefaultDelebiConfig().KeyCodes,
                };

                setting.ShareJipperResourcePack = old.ShareJipperResourcePack;

                // Write new format immediately
                var serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
                var newJson = JsonConvert.SerializeObject(setting, serializerSettings);
                File.WriteAllText(filepath, newJson);
                Main.Logger?.Log("[Setting.Load] Migration complete — config saved in v2 format.");

                return setting;
            }
            catch (Exception ex)
            {
                Main.Logger?.Log($"[Setting.Load] Migration failed: {ex.Message}");
                var fallback = new Setting();
                fallback.YoonConfig = DefaultYoonConfig();
                fallback.LineConfig = DefaultLineConfig();
                fallback.DelebiConfig = DefaultDelebiConfig();
                return fallback;
            }
        }

        /// <summary>
        /// v1 flat-field schema — only used for JSON deserialization during migration.
        /// </summary>
#pragma warning disable CS0649
        private class SettingV1
        {
            public CharacterType Character = CharacterType.Yoon;
            public float Size = 1f;
            public float LocationX = 0f;
            public float LocationY = 1f;
            public float NervousBPM = 300f;
            public bool FlipHorizontal = false;
            public bool HideDesk = false;
            public bool HideFeet = false;
            public bool HideFeetKeyboard = false;
            public bool ShareJipperResourcePack = true;

            public KeyCode[] YoonKeyCodes;
            public KeyCode[] LineKeyCodes;
            public KeyCode[] DelebiKeyCodes;
            public KeyCode[] FKeyCodes;
        }
#pragma warning restore CS0649
    }
}
