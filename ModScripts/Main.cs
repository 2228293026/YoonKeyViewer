using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using YoonKeyViewer.Component;

using UnityModManagerNet;

// Jongyeol if you are reading this, excuse my bad code :p
namespace YoonKeyViewer
{
    public static class Main
    {
        public static bool IsEnabled = false;
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static UnityModManager.ModEntry ModEntry;
        public static Harmony harmony;
        public static Setting setting;
        public static scrYoonKeyViewer KeyViewer;
        public static scrLineKeyViewer KeyViewerLine;
        public static scrDelebiKeyViewer KeyViewerDelebi;
        private static int SelectedKey;
        private static int WinAPICool;
        private static bool[] KeyPressed = null;
        public static string ModPath;

        internal static void Setup(UnityModManager.ModEntry modEntry)
        {
            setting = Setting.Load(modEntry);
            ModEntry = modEntry;
            Logger = modEntry.Logger;
            ModPath = modEntry.Path;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUpdate = OnUpdate;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            IsEnabled = value;
            if (value)
            {
                harmony = new Harmony(modEntry.Info.Id);
                PatchManager.Initialize(harmony);
                VersionSafe.Setup();
                GameLifecyclePatches.RegisterAll();

                if (setting.Character == CharacterType.Yoon)
                {
                    YoonBundleManager.Instance = new YoonBundleManager();
                    GameObject gameObject = UnityEngine.Object.Instantiate(YoonBundleManager.Instance.KeyViewerObject);
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    KeyViewer = gameObject.GetComponent<scrYoonKeyViewer>();
                    //KeyViewer = ViewerSetup.SetupYoon(gameObject);
                    KeyViewer.sizeTransform.localScale = new Vector3(setting.Size, setting.Size);
                    if (setting.FlipHorizontal) KeyViewer.sizeTransform.eulerAngles = new Vector3(0, 180, 0);
                    if (setting.HideDesk) KeyViewer.Table.enable = 0;
                }
                else if (setting.Character == CharacterType.Line)
                {
                    LineBundleManager.Instance = new LineBundleManager();
                    GameObject gameObject = UnityEngine.Object.Instantiate(LineBundleManager.Instance.KeyViewerObject);
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    KeyViewerLine = gameObject.GetComponent<scrLineKeyViewer>();
                    //KeyViewerLine = ViewerSetup.SetupLine(gameObject);
                    KeyViewerLine.mainImage.sprite = setting.HideDesk ? LineBundleManager.Instance.Line : LineBundleManager.Instance.LineTable;
                    KeyViewerLine.sizeTransform.localScale = new Vector3(setting.Size, setting.Size);
                    if (setting.FlipHorizontal) KeyViewerLine.sizeTransform.eulerAngles = new Vector3(0, 180, 0);
                }
                else
                {
                    DelebiBundleManager.Instance = new DelebiBundleManager();
                    GameObject gameObject = UnityEngine.Object.Instantiate(DelebiBundleManager.Instance.KeyViewerObject);
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    KeyViewerDelebi = gameObject.GetComponent<scrDelebiKeyViewer>();
                    KeyViewerDelebi.sizeTransform.localScale = new Vector3(setting.Size, setting.Size);
                    if (setting.FlipHorizontal) KeyViewerDelebi.sizeTransform.eulerAngles = new Vector3(0, 180, 0);
                }
                UpdateLocation();
                PatchManager.ApplyAll();

                KeyInputManager.NeedsReset = true;
            }
            else
            {
                if (KeyViewer)
                {
                    UnityEngine.Object.Destroy(KeyViewer.gameObject);
                    KeyViewer = null;
                }
                if (KeyViewerLine)
                {
                    UnityEngine.Object.Destroy(KeyViewerLine.gameObject);
                    KeyViewerLine = null;
                }
                if (KeyViewerDelebi)
                {
                    UnityEngine.Object.Destroy(KeyViewerDelebi.gameObject);
                    KeyViewerDelebi = null;
                }
                KeyInputManager.NeedsReset = true;
                YoonBundleManager.Instance?.Dispose();
                LineBundleManager.Instance?.Dispose();
                DelebiBundleManager.Instance?.Dispose();
                PatchManager.UnpatchAll();
            }
            return true;
        }

        private static void SwitchCharacter()
        {
            // Destroy current viewer immediately (not frame-delayed)
            if (KeyViewer)
            {
                UnityEngine.Object.DestroyImmediate(KeyViewer.gameObject);
                KeyViewer = null;
            }
            if (KeyViewerLine)
            {
                UnityEngine.Object.DestroyImmediate(KeyViewerLine.gameObject);
                KeyViewerLine = null;
            }
            if (KeyViewerDelebi)
            {
                UnityEngine.Object.DestroyImmediate(KeyViewerDelebi.gameObject);
                KeyViewerDelebi = null;
            }

            // Create new character's viewer
            if (setting.Character == CharacterType.Yoon)
            {
                if (YoonBundleManager.Instance == null)
                    YoonBundleManager.Instance = new YoonBundleManager();
                GameObject go = UnityEngine.Object.Instantiate(YoonBundleManager.Instance.KeyViewerObject);
                UnityEngine.Object.DontDestroyOnLoad(go);
                KeyViewer = go.GetComponent<scrYoonKeyViewer>();
                //KeyViewer = ViewerSetup.SetupYoon(go);
                if (setting.HideDesk) KeyViewer.Table.enable = 0;
            }
            else if (setting.Character == CharacterType.Line)
            {
                if (LineBundleManager.Instance == null)
                    LineBundleManager.Instance = new LineBundleManager();
                GameObject go = UnityEngine.Object.Instantiate(LineBundleManager.Instance.KeyViewerObject);
                UnityEngine.Object.DontDestroyOnLoad(go);
                KeyViewerLine = go.GetComponent<scrLineKeyViewer>();
                //KeyViewerLine = ViewerSetup.SetupLine(go);
                KeyViewerLine.mainImage.sprite = setting.HideDesk
                    ? LineBundleManager.Instance.Line
                    : LineBundleManager.Instance.LineTable;
            }
            else
            {
                if (DelebiBundleManager.Instance == null)
                    DelebiBundleManager.Instance = new DelebiBundleManager();
                GameObject go = UnityEngine.Object.Instantiate(DelebiBundleManager.Instance.KeyViewerObject);
                UnityEngine.Object.DontDestroyOnLoad(go);
                KeyViewerDelebi = go.GetComponent<scrDelebiKeyViewer>();
                KeyViewerDelebi.sizeTransform.localScale = new Vector3(setting.Size, setting.Size);
                if (setting.FlipHorizontal) KeyViewerDelebi.sizeTransform.eulerAngles = new Vector3(0, 180, 0);
            }

            // Apply size & flip
            var scale = new Vector3(setting.Size, setting.Size);
            var angles = new Vector3(0, setting.FlipHorizontal ? 180 : 0, 0);
            if (KeyViewer) { KeyViewer.sizeTransform.localScale = scale; KeyViewer.sizeTransform.eulerAngles = angles; }
            if (KeyViewerLine) { KeyViewerLine.sizeTransform.localScale = scale;
                KeyViewerLine.mainImage.sprite = setting.HideDesk ? LineBundleManager.Instance.Line : LineBundleManager.Instance.LineTable; KeyViewerLine.sizeTransform.eulerAngles = angles; }

            UpdateLocation();
            KeyInputManager.NeedsReset = true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label($"{setting.GetLocalized("ykv.settingslabel")}");

            // Character selector
            string[] charNames = { "Yoon", "Line", "Delebi" };
            int charIdx = (int)setting.Character;
            int newCharIdx = GUILayout.SelectionGrid(charIdx, charNames, 3);
            if (newCharIdx != charIdx)
            {
                setting.Character = (CharacterType)newCharIdx;
                setting.Save(ModEntry);
                SwitchCharacter();
            }

            float newSize = NamedSlider("Size:", setting.Size, 0, 2, 150);
            if (newSize != setting.Size)
            {
                setting.Size = newSize;
                var scale = new Vector3(setting.Size, setting.Size);
                if (KeyViewer?.sizeTransform) KeyViewer.sizeTransform.localScale = scale;
                if (KeyViewerLine?.sizeTransform) KeyViewerLine.sizeTransform.localScale = scale;
                if (KeyViewerDelebi?.sizeTransform) KeyViewerDelebi.sizeTransform.localScale = scale;
                UpdateLocation();
            }
            float newLocationX = NamedSlider($"{setting.GetLocalized("ykv.posx")}:", setting.LocationX, 0, 1, 250);
            if (newLocationX != setting.LocationX)
            {
                setting.LocationX = newLocationX;
                UpdateLocation();
            }
            float newLocationY = NamedSlider($"{setting.GetLocalized("ykv.posy")}:", setting.LocationY, 0, 1, 250);
            if (newLocationY != setting.LocationY)
            {
                setting.LocationY = newLocationY;
                UpdateLocation();
            }
            // Nervous BPM (Yoon only)
            if (setting.Character == CharacterType.Yoon)
            {
                float newNervousBPM = NamedSlider($"{setting.GetLocalized("ykv.nervousbpm")}:", setting.NervousBPM, 1, 1000, 250);
                newNervousBPM = Mathf.Max(1, newNervousBPM);
                if (newNervousBPM != setting.NervousBPM)
                {
                    setting.NervousBPM = newNervousBPM;
                }
            }
            bool newFlipHorizontal = GUILayout.Toggle(setting.FlipHorizontal, $"{setting.GetLocalized("ykv.fliphorizontal")}:");
            if (newFlipHorizontal != setting.FlipHorizontal)
            {
                setting.FlipHorizontal = newFlipHorizontal;
                var angles = new Vector3(0, setting.FlipHorizontal ? 180 : 0, 0);
                if (KeyViewer?.sizeTransform) KeyViewer.sizeTransform.eulerAngles = angles;
                if (KeyViewerLine?.sizeTransform) KeyViewerLine.sizeTransform.eulerAngles = angles;
                if (KeyViewerDelebi?.sizeTransform) KeyViewerDelebi.sizeTransform.eulerAngles = angles;
                KeyInputManager.NeedsReset = true;
            }
            if (setting.Character != CharacterType.Delebi)
            {
                bool newHideDesk = GUILayout.Toggle(setting.HideDesk, $"{setting.GetLocalized("ykv.hidedesk")}:");
                if (newHideDesk != setting.HideDesk)
                {
                    setting.HideDesk = newHideDesk;
                    if (KeyViewer) KeyViewer.Table.enable = setting.HideDesk ? (sbyte)0 : (sbyte)1;
                    if (KeyViewerLine) KeyViewerLine.mainImage.sprite = setting.HideDesk
                        ? LineBundleManager.Instance.Line : LineBundleManager.Instance.LineTable;
                }
            }

            bool oldHideFeet = setting.HideFeet;
            if (setting.Character == CharacterType.Yoon)
            {
                bool newHideLegs = GUILayout.Toggle(setting.HideFeet, $"{setting.GetLocalized("ykv.hidelegs")}:");
                if (newHideLegs != setting.HideFeet)
                {
                    setting.HideFeet = newHideLegs;
                    if (KeyViewer)
                    {
                        KeyViewer.leftLeg.enable = setting.HideFeet ? (sbyte)0 : (sbyte)1;
                        KeyViewer.rightLeg.enable = setting.HideFeet ? (sbyte)0 : (sbyte)1;
                        KeyViewer.FeetKeyboard.enable = setting.HideFeet ? (sbyte)0 : setting.HideFeetKeyboard ? (sbyte)0 : (sbyte)1;

                    }
                    KeyInputManager.NeedsReset = true;
                }
                if (!oldHideFeet)
                {
                    bool newHideFeetKeyboard = GUILayout.Toggle(setting.HideFeetKeyboard, $"{setting.GetLocalized("ykv.hidefeetkeyboard")}");
                    if (newHideFeetKeyboard != setting.HideFeetKeyboard)
                    {
                        setting.HideFeetKeyboard = newHideFeetKeyboard;
                        if (KeyViewer) KeyViewer.FeetKeyboard.enable = newHideFeetKeyboard ? (sbyte)0 : (sbyte)1;
                        KeyInputManager.NeedsReset = true;
                    }
                }
            }
            
            // Top Row
            GUILayout.Space(18f);
            GUILayout.Label(setting.GetLocalized("ykv.toprow"));
            GUILayout.BeginHorizontal();
            int keyCount = setting.Character == CharacterType.Delebi ? 10 : 16;
            for (int i = 0; i < Math.Min(8, keyCount); i++) CreateButton(i);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Bottom Row
            GUILayout.Label(setting.GetLocalized("ykv.bottomrow"));
            GUILayout.BeginHorizontal();
            if (setting.Character == CharacterType.Delebi)
            {
                for (int i = 8; i < 10; i++) CreateButton(i);
            }
            else
            {
                for (int i = 8; i < 16; i++) CreateButton(KeyInputManager.HandLocation[i]);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Feet Keyboard (Yoon only)
            if (setting.Character == CharacterType.Yoon && !oldHideFeet && !setting.HideFeetKeyboard)
            {
                GUILayout.Label(setting.GetLocalized("ykv.feetkeyboard"));
                GUILayout.BeginHorizontal();
                for (int i = 0; i < 4; i++) CreateFootButton(i);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (SelectedKey == -1 || KeyPressed == null) return;
            GUILayout.Label($"<b>{setting.GetLocalized("ykv.pressakey")}</b>");
            if (Input.anyKeyDown)
            {
                foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
                {
                    if (!Input.GetKeyDown(keyCode)) continue;
                    SetupKey(keyCode);
                    break;
                }
            }
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    if ((KeyInputManager.GetAsyncKeyState(i) & 0x8000) != 0 == KeyPressed[i]) continue;
                    if (KeyPressed[i])
                    {
                        KeyPressed[i] = false;
                        WinAPICool = 0;
                        continue;
                    }
                    if (WinAPICool++ < 6) break;
                    KeyCode keyCode = (KeyCode)i + 0x1000;
                    SetupKey(keyCode);
                    break;
                }
            }
        }

        private static void CreateButton(int i)
        {
            var codes = setting.Character == CharacterType.Yoon ? setting.YoonKeyCodes
                : setting.Character == CharacterType.Line ? setting.LineKeyCodes
                : setting.DelebiKeyCodes;
            if (!GUILayout.Button(Bold(KeyToString(codes[i]), i == SelectedKey))) return;
            SelectedKey = i;
            WinAPICool = 0;
            KeyPressed = new bool[256];
            for (int i2 = 0; i2 < 256; i2++) KeyPressed[i2] = (KeyInputManager.GetAsyncKeyState(i2) & 0x8000) != 0;
        }
        private static void CreateFootButton(int i)
        {
            if (!GUILayout.Button(Bold(KeyToString(setting.FKeyCodes[i]), i + 100 == SelectedKey))) return;
            SelectedKey = i + 100; // Offset to distinguish from hand keys
            WinAPICool = 0;
            KeyPressed = new bool[256];
            for (int i2 = 0; i2 < 256; i2++)
                KeyPressed[i2] = (KeyInputManager.GetAsyncKeyState(i2) & 0x8000) != 0;
        }

        private static void SetupKey(KeyCode keyCode)
        {
            if (SelectedKey >= 100)
            {
                setting.FKeyCodes[SelectedKey - 100] = keyCode;
            }
            else if (setting.Character == CharacterType.Delebi)
            {
                setting.DelebiKeyCodes[SelectedKey] = keyCode;
            }
            else if (setting.Character == CharacterType.Line)
            {
                setting.LineKeyCodes[SelectedKey] = keyCode;
            }
            else
            {
                setting.YoonKeyCodes[SelectedKey] = keyCode;
            }

            SelectedKey = -4;
            WinAPICool = 0;
            KeyPressed = null;
            setting.Save(ModEntry);
        }

        private static string Bold(string text, bool bold) => !bold ? text : $"<b>{text}</b>";

        private static string KeyToString(KeyCode keyCode) => (int)keyCode < 0x1000 ? keyCode.ToString() : ((System.Windows.Forms.Keys)(keyCode - 0x1000)).ToString();

        private static float _nextWinkTime = 1f;
        private static float _winkEndTime;
        private static bool _winkInProgress;

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            if (!IsEnabled) return;
            KeyInputManager.Update();
            UpdateWinking();
        }

        private static void UpdateWinking()
        {
            if (setting.Character == CharacterType.Line)
                UpdateWinkingLine();
            else if (setting.Character == CharacterType.Delebi)
                UpdateWinkingDelebi();
            else
                UpdateWinkingYoon();
        }

        private static void UpdateWinkingYoon()
        {
            if (KeyViewer == null) return;

            if (_winkInProgress)
            {
                if (Time.time >= _winkEndTime)
                {
                    _winkInProgress = false;
                    if (KeyViewer.winkOn)
                    {
                        KeyViewer.winkOn = false;
                        KeyViewer.Yoon.sprite = YoonBundleManager.Instance.YoonIdle;
                        KeyViewer.YoonSmash.enable = 0;
                        KeyViewer.YoonClear.enable = 0;
                        KeyViewer.Table.enable = setting.HideDesk ? (sbyte)0 : (sbyte)1;
                    }
                    _nextWinkTime = Time.time + UnityEngine.Random.Range(3f, 7f);
                }
                return;
            }

            if (KeyViewer.isSmashing || KeyViewer.gameResult || KeyViewer.isNervous)
            {
                _nextWinkTime = Time.time + UnityEngine.Random.Range(1f, 3f);
                return;
            }

            if (Time.time >= _nextWinkTime)
            {
                _winkInProgress = true;
                _winkEndTime = Time.time + UnityEngine.Random.Range(0.1f, 0.25f);
                KeyViewer.winkOn = true;
                KeyViewer.Yoon.sprite = YoonBundleManager.Instance.YoonIdleWink;
                KeyViewer.YoonSmash.enable = 0;
                KeyViewer.YoonClear.enable = 0;
                KeyViewer.Table.enable = setting.HideDesk ? (sbyte)0 : (sbyte)1;
            }
        }

        private static void UpdateWinkingLine()
        {
            var lv = KeyViewerLine;
            if (lv == null) return;

            if (_winkInProgress)
            {
                if (Time.time >= _winkEndTime)
                {
                    _winkInProgress = false;
                    if (lv.winkOn)
                    {
                        lv.winkOn = false;
                        lv.mainImage.sprite = setting.HideDesk ? LineBundleManager.Instance.Line : LineBundleManager.Instance.LineTable;
                    }
                    _nextWinkTime = Time.time + UnityEngine.Random.Range(3f, 7f);
                }
                return;
            }

            if (lv.headOn || lv.gameResult)
            {
                _nextWinkTime = Time.time + UnityEngine.Random.Range(1f, 3f);
                return;
            }

            if (Time.time >= _nextWinkTime)
            {
                _winkInProgress = true;
                _winkEndTime = Time.time + UnityEngine.Random.Range(0.1f, 0.25f);
                lv.winkOn = true;
                lv.mainImage.sprite = setting.HideDesk ? LineBundleManager.Instance.LineWink : LineBundleManager.Instance.LineWinkTable;
                lv.mainImage.enable = 1;
            }
        }

        private static void UpdateWinkingDelebi()
        {
            var dv = KeyViewerDelebi;
            if (dv == null) return;

            if (_winkInProgress)
            {
                if (Time.time >= _winkEndTime)
                {
                    _winkInProgress = false;
                    if (dv.winkOn)
                    {
                        dv.winkOn = false;
                        dv.Delebi.sprite = DelebiBundleManager.Instance.DelebiIdle;
                        dv.DelebiSmash.enable = 0;
                        dv.DelebiClear.enable = 0;
                    }
                    _nextWinkTime = Time.time + UnityEngine.Random.Range(3f, 7f);
                }
                return;
            }

            if (dv.isSmashing || dv.gameResult)
            {
                _nextWinkTime = Time.time + UnityEngine.Random.Range(1f, 3f);
                return;
            }

            if (Time.time >= _nextWinkTime)
            {
                _winkInProgress = true;
                _winkEndTime = Time.time + UnityEngine.Random.Range(0.1f, 0.25f);
                dv.winkOn = true;
                dv.Delebi.sprite = DelebiBundleManager.Instance.DelebiIdleWink;
                dv.DelebiSmash.enable = 0;
                dv.DelebiClear.enable = 0;
            }
        }

        private static void UpdateLocation()
        {
            float y = 1 - setting.LocationY;
            RectTransform rectTransform = KeyViewer?.locationTransform ?? KeyViewerLine?.locationTransform ?? KeyViewerDelebi?.locationTransform;
            if (rectTransform == null) return;
            rectTransform.sizeDelta = new Vector2(540 * setting.Size, 420 * setting.Size);
            rectTransform.pivot = new Vector2(setting.LocationX, y);
            rectTransform.anchoredPosition = new Vector2(setting.LocationX * 1920, y * 1080);
        }

        public static float NamedSlider(string name, float value, float leftValue, float rightValue, float sliderWidth,
    float roundNearest = 0, float labelWidth = 0, string valueFormat = "{0}", string endformat = "", float inputFieldWidth = 60f,
    bool useInputFieldAsLabel = true)
        {
            GUILayout.BeginHorizontal();
            float newValue = NamedSliderContent(
                name,
                value,
                leftValue,
                rightValue,
                sliderWidth,
                roundNearest,
                labelWidth,
                valueFormat,
                endformat,
                inputFieldWidth,
                useInputFieldAsLabel
            );
            GUILayout.EndHorizontal();
            return newValue;
        }

        private static float NamedSliderContent(string name, float value, float leftValue, float rightValue, float sliderWidth,
    float roundNearest = 0, float labelWidth = 0, string valueFormat = "{0}", string endformat = "", float inputFieldWidth = 60f,
    bool useInputFieldAsLabel = true)
        {
            if (labelWidth == 0)
            {
                GUILayout.Label(name);
            }
            else
            {
                GUILayout.Label(name, GUILayout.Width(labelWidth));
            }

            GUILayout.Space(4f);

            // Slider
            float newValue = GUILayout.HorizontalSlider(value, leftValue, rightValue, GUILayout.Width(sliderWidth));
            if (roundNearest != 0)
            {
                newValue = Mathf.Round(newValue / roundNearest) * roundNearest;
            }

            GUILayout.Space(8f);

            if (useInputFieldAsLabel)
            {
                // Input field for exact numeric value
                string valueStr = GUILayout.TextField(value.ToString("F2"), GUILayout.Width(inputFieldWidth));

                // Only process if the text has actually changed
                if (valueStr != value.ToString("F2"))
                {
                    // Check for valid float string (allowing negative and decimal)
                    if (string.IsNullOrWhiteSpace(valueStr))
                    {
                        newValue = value; // Reset to previous value if empty
                    }
                    else if (IsValidFloatString(valueStr) && float.TryParse(valueStr, out float parsed))
                    {
                        newValue = parsed;
                        if (roundNearest != 0)
                        {
                            newValue = Mathf.Round(newValue / roundNearest) * roundNearest;
                        }
                    }
                    else
                    {
                        newValue = value; // Reset to previous value if invalid
                    }
                }

                if (!string.IsNullOrEmpty(endformat))
                {
                    GUILayout.Space(4f);
                    GUILayout.Label(endformat);
                }
            }
            else
            {
                GUILayout.Label(string.Format(valueFormat, newValue) + endformat, GUILayout.Width(40f));
            }

            GUILayout.FlexibleSpace();
            return newValue;
        }

        // Helper method to check if a string could be a valid float (allowing negative and decimal)
        private static bool IsValidFloatString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check for multiple decimal points or minus signs
            if (input.Count(c => c == '.') > 1 || input.Count(c => c == '-') > 1)
                return false;

            // Check if minus is in the correct position
            if (input.Contains('-') && input.IndexOf('-') != 0)
                return false;

            // Check if all characters are digits, minus, or decimal point
            foreach (char c in input)
            {
                if (!char.IsDigit(c) && c != '.' && c != '-')
                    return false;
            }

            return true;
        }
        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            setting.Save(modEntry);
        }
    }
}
