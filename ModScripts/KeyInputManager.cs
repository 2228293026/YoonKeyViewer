using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using YoonKeyViewer.Component;
using UnityEngine;

namespace YoonKeyViewer
{
    public static class KeyInputManager
    {
        public static readonly int[] HandLocation = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 12, 13, 9, 8, 10, 11, 14, 15 };
        public static readonly int[] HandLocationDelebi = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public static readonly int[] LegLocation = new int[] { 0, 1, 2, 3 };

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        private static int KeyCodeToVK(KeyCode keyCode)
        {
            int c = (int)keyCode;
            if (c >= 48 && c <= 57) return c;
            if (c >= 97 && c <= 122) return c - 32;
            if (c >= 276 && c <= 279) return c - 239;
            if (c >= 282 && c <= 293) return c - 170;
            if (c == 8 || c == 9 || c == 12 || c == 13 || c == 19) return c;
            if (c == 27 || c == 32) return c;
            switch (keyCode)
            {
                case KeyCode.Comma: return 188;
                case KeyCode.Period: return 190;
                case KeyCode.Semicolon: return 186;
                case KeyCode.Slash: return 191;
                case KeyCode.BackQuote: return 192;
                case KeyCode.LeftBracket: return 219;
                case KeyCode.Backslash: return 220;
                case KeyCode.RightBracket: return 221;
                case KeyCode.Quote: return 222;
                case KeyCode.Minus: return 189;
                case KeyCode.Equals: return 187;
                case KeyCode.LeftShift: return 160;
                case KeyCode.RightShift: return 161;
                case KeyCode.LeftControl: return 162;
                case KeyCode.RightControl: return 163;
                case KeyCode.LeftAlt: return 164;
                case KeyCode.RightAlt: return 165;
                case KeyCode.Delete: return 46;   // VK_DELETE
                case KeyCode.Insert: return 45;   // VK_INSERT
                case KeyCode.Home: return 36;   // VK_HOME
                case KeyCode.End: return 35;   // VK_END
                case KeyCode.PageUp: return 33;   // VK_PRIOR
                case KeyCode.PageDown: return 34;   // VK_NEXT
                case KeyCode.CapsLock: return 20;   // VK_CAPITAL
                case KeyCode.Numlock: return 144;  // VK_NUMLOCK
                case KeyCode.ScrollLock: return 145; // VK_SCROLL
                case KeyCode.Print: return 44;   // VK_SNAPSHOT (PrintScreen)
                case KeyCode.Pause: return 19;   // VK_PAUSE
                case KeyCode.Tab: return 9;    // VK_TAB
                case KeyCode.Space: return 32;   // VK_SPACE
                case KeyCode.Return: return 13;   // VK_RETURN
                case KeyCode.Escape: return 27;   // VK_ESCAPE
                case KeyCode.Keypad0: return 96;   // VK_NUMPAD0
                case KeyCode.Keypad1: return 97;
                case KeyCode.Keypad2: return 98;
                case KeyCode.Keypad3: return 99;
                case KeyCode.Keypad4: return 100;
                case KeyCode.Keypad5: return 101;
                case KeyCode.Keypad6: return 102;
                case KeyCode.Keypad7: return 103;
                case KeyCode.Keypad8: return 104;
                case KeyCode.Keypad9: return 105;
                case KeyCode.KeypadDivide: return 111; // VK_DIVIDE
                case KeyCode.KeypadMultiply: return 106; // VK_MULTIPLY
                case KeyCode.KeypadMinus: return 109; // VK_SUBTRACT
                case KeyCode.KeypadPlus: return 107; // VK_ADD
                case KeyCode.KeypadPeriod: return 110; // VK_DECIMAL
                case KeyCode.KeypadEnter: return 13;  // VK_RETURN
                case KeyCode.LeftWindows: return 91;   // VK_LWIN
                case KeyCode.RightWindows: return 92;  // VK_RWIN
                case KeyCode.Menu: return 93;           // VK_APPS (Application/Menu key)
            }
            return (int)keyCode;
        }

        private static bool CheckKey(KeyCode keyCode) => (int)keyCode >= 0x1000
            ? GetAsyncKeyState((int)keyCode - 0x1000) != 0
            : GetAsyncKeyState(KeyCodeToVK(keyCode)) != 0;

        // ── 主线程内部状态（只在 OnUpdate 访问） ──
        private static readonly bool[] _prevHand = new bool[16];
        private static readonly bool[] _prevFoot = new bool[4];
        private static readonly bool[] _hBuffer = new bool[16];
        private static readonly bool[] _fBuffer = new bool[4];
        private static readonly List<int> _leftPressed = new(8);
        private static readonly List<int> _rightPressed = new(8);
        private static int _mainCount;

        public static bool NeedsReset;

        // ── 主线程调用（Main.OnUpdate）：轮询按键 + 更新 Unity UI ──
        public static void Update()
        {
            if (Main.setting == null) return;
            var s = Main.setting;
            var cfg = s.Current;

            int[] handLoc = s.Character == CharacterType.Delebi ? HandLocationDelebi : HandLocation;
            int keyCount = s.Character == CharacterType.Delebi ? 10 : 16;
            var codes = cfg.KeyCodes;
            for (int i = 0; i < keyCount; i++)
                _hBuffer[i] = CheckKey(codes[handLoc[i]]);
            if (s.Character != CharacterType.Delebi)
            {
                var fCodes = cfg.FKeyCodes;
                for (int i = 0; i < 4; i++)
                    _fBuffer[i] = CheckKey(fCodes[LegLocation[i]]);
            }

            if (s.Character == CharacterType.Line)
                ApplyKeyStatesLine(_hBuffer);
            else if (s.Character == CharacterType.Delebi)
                ApplyKeyStatesDelebi(_hBuffer);
            else
                ApplyKeyStatesYoon(_hBuffer, _fBuffer);
        }

        #region Yoon key state apply (main thread)
        private static void ApplyKeyStatesYoon(bool[] h, bool[] f)
        {
            var v = Main.KeyViewer;
            if (v == null) return;
            var s = Main.setting;

            var cfg = s.Current;
            bool flipH = cfg.FlipHorizontal;
            bool hideDesk = cfg.HideDesk;
            bool hideFeet = cfg.HideFeet;
            bool hideFeetKb = cfg.HideFeetKeyboard;

            // ── Reset ──
            if (NeedsReset)
            {
                for (int i = 0; i < 16; i++) { if (v.keys[i] != null) v.keys[i].enable = 0; _prevHand[i] = false; }
                for (int i = 0; i < 4; i++) { if (v.fKeys[i] != null) v.fKeys[i].enable = 0; _prevFoot[i] = false; }
                _leftPressed.Clear(); _rightPressed.Clear();
                if (v.leftHand) v.leftHand.sprite = YoonBundleManager.Instance.UnpressedKeySprites[0];
                if (v.rightHand) v.rightHand.sprite = YoonBundleManager.Instance.UnpressedKeySprites[1];
                if (v.leftLeg) v.leftLeg.sprite = YoonBundleManager.Instance.FUnpressedKeySprites[0];
                if (v.rightLeg) v.rightLeg.sprite = YoonBundleManager.Instance.FUnpressedKeySprites[1];
                _mainCount = 0; NeedsReset = false;
            }

            // ── 手键 ──
            for (int i = 0; i < 16; i++)
            {
                bool cur = h[i];
                if (cur == _prevHand[i]) continue;
                int num = flipH ? (i < 8 ? 7 : 23) - i : i;
                bool left = num < 4 || (num >= 8 && num < 12);
                var key = v.keys[num];
                if (key == null) continue;
                _prevHand[i] = cur;
                key.enable = (sbyte)(cur ? 1 : 0);
                var pl = left ? _leftPressed : _rightPressed;
                if (cur)
                {
                    pl.Add(num);
                    (left ? v.leftHand : v.rightHand).sprite = YoonBundleManager.Instance.PressedKeySprites[num];
                }
                else
                {
                    pl.Remove(num);
                    (left ? v.leftHand : v.rightHand).sprite =
                        pl.Count == 0 ? YoonBundleManager.Instance.UnpressedKeySprites[left ? 0 : 1]
                                      : YoonBundleManager.Instance.PressedKeySprites[pl[pl.Count - 1]];
                }
                if (i >= 8) continue;
                if (cur) _mainCount++; else _mainCount--;
                if (v.gameResult) continue;

                if (_mainCount < 8)
                {
                    if (!v.isSmashing) continue;
                    v.YoonSmash.sprite = v.YoonSmash.image.sprite = YoonBundleManager.Instance.YoonSmash;
                    v.Yoon.enable = 1; v.leftHand.enable = 1; v.rightHand.enable = 1; v.YoonSmash.enable = 0;
                    if (hideDesk) { if (v.winkOn) { v.Table.enable = 0; v.winkOn = false; } }
                    else { v.winkOn = false; }
                    v.isSmashing = false;
                }
                else if (!v.isSmashing)
                {
                    v.Yoon.enable = 0; v.leftHand.enable = 0; v.rightHand.enable = 0;
                    v.YoonSmash.enable = 1; v.isSmashing = true;
                }
            }

            // ── 脚键（Yoon only） ──
            if (!hideFeet && !hideFeetKb)
            {
                if (v.FeetKeyboard && v.FeetKeyboard.enable == 0) v.FeetKeyboard.enable = 1;
                for (int i = 0; i < 4; i++)
                {
                    bool cur = f[i];
                    if (cur == _prevFoot[i]) continue;
                    _prevFoot[i] = cur;
                    if (v.fKeys[i] != null) v.fKeys[i].enable = (sbyte)(cur ? 1 : 0);
                }
                if (v.leftLeg)
                    v.leftLeg.sprite = (f[0] || f[1]) ? YoonBundleManager.Instance.FPressedKeySprites[0]
                                                      : YoonBundleManager.Instance.FUnpressedKeySprites[0];
                if (v.rightLeg)
                    v.rightLeg.sprite = (f[2] || f[3]) ? YoonBundleManager.Instance.FPressedKeySprites[1]
                                                       : YoonBundleManager.Instance.FUnpressedKeySprites[1];
            }
        }
        #endregion

        #region Line key state apply (main thread)
        private static void ApplyKeyStatesLine(bool[] h)
        {
            var v = Main.KeyViewerLine;
            if (v == null) return;
            var s = Main.setting;
            var cfg = s.Current;
            bool flipH = cfg.FlipHorizontal;
            bool hideDesk = cfg.HideDesk;

            if (NeedsReset)
            {
                for (int i = 0; i < 16; i++) { if (v.keys[i] != null) v.keys[i].enable = 0; _prevHand[i] = false; }
                _leftPressed.Clear(); _rightPressed.Clear();
                if (v.leftHand) v.leftHand.sprite = LineBundleManager.Instance.UnpressedKeySprites[0];
                if (v.rightHand) v.rightHand.sprite = LineBundleManager.Instance.UnpressedKeySprites[1];
                _mainCount = 0; NeedsReset = false;
            }

            for (int i = 0; i < 16; i++)
            {
                bool cur = h[i];
                if (cur == _prevHand[i]) continue;
                int num = flipH ? (i < 8 ? 7 : 23) - i : i;
                bool left = num >= 12 || (num >= 4 && num < 8);
                var key = v.keys[num];
                if (key == null) continue;
                _prevHand[i] = cur;
                key.enable = (sbyte)(cur ? 1 : 0);
                var pl = left ? _leftPressed : _rightPressed;
                if (cur)
                {
                    pl.Add(num);
                    (left ? v.leftHand : v.rightHand).sprite = LineBundleManager.Instance.PressedKeySprites[num];
                }
                else
                {
                    pl.Remove(num);
                    (left ? v.leftHand : v.rightHand).sprite =
                        pl.Count == 0 ? LineBundleManager.Instance.UnpressedKeySprites[left ? 0 : 1]
                                      : LineBundleManager.Instance.PressedKeySprites[pl[pl.Count - 1]];
                }
                if (i >= 8) continue;
                if (cur) _mainCount++; else _mainCount--;
                if (v.gameResult) continue;

                if (_mainCount < 8)
                {
                    if (!v.headOn) continue;
                    v.head.enable = 0; v.leftHand.enable = 1; v.rightHand.enable = 1;
                    if (hideDesk)
                    {
                        if (v.winkOn) { v.mainImage.sprite = LineBundleManager.Instance.Line; v.winkOn = false; }
                        v.mainImage.enable = 1;
                    }
                    else { v.mainImage.sprite = LineBundleManager.Instance.LineTable; v.winkOn = false; }
                    v.headOn = false;
                }
                else if (!v.headOn)
                {
                    v.head.enable = 1; v.leftHand.enable = 0; v.rightHand.enable = 0;
                    if (hideDesk) v.mainImage.enable = 0;
                    else v.mainImage.sprite = LineBundleManager.Instance.Table;
                    v.headOn = true;
                }
            }
        }
        #endregion

        #region Delebi key state apply (main thread)
        private static void ApplyKeyStatesDelebi(bool[] h)
        {
            var v = Main.KeyViewerDelebi;
            if (v == null) return;
            var s = Main.setting;
            bool flipH = s.Current.FlipHorizontal;

            if (NeedsReset)
            {
                for (int i = 0; i < 10; i++)
                    { if (v.keys[i] != null) v.keys[i].enable = 0; _prevHand[i] = false; }
                _leftPressed.Clear(); _rightPressed.Clear();
                if (v.leftHand) v.leftHand.sprite = DelebiBundleManager.Instance.UnpressedKeySprites[0];
                if (v.rightHand) v.rightHand.sprite = DelebiBundleManager.Instance.UnpressedKeySprites[1];
                _mainCount = 0; NeedsReset = false;
            }

            for (int i = 0; i < 10; i++)
            {
                bool cur = h[i];
                if (cur == _prevHand[i]) continue;
                int num = flipH ? (i < 8 ? 7 : 9 - (i - 8)) : i;
                bool left = num < 4 || num == 8;
                var key = v.keys[num];
                if (key == null) continue;
                _prevHand[i] = cur;
                key.enable = (sbyte)(cur ? 1 : 0);
                var pl = left ? _leftPressed : _rightPressed;
                if (cur)
                {
                    pl.Add(num);
                    (left ? v.leftHand : v.rightHand).sprite
                        = DelebiBundleManager.Instance.PressedKeySprites[num];
                }
                else
                {
                    pl.Remove(num);
                    (left ? v.leftHand : v.rightHand).sprite
                        = pl.Count == 0
                            ? DelebiBundleManager.Instance.UnpressedKeySprites[left ? 0 : 1]
                            : DelebiBundleManager.Instance.PressedKeySprites[pl[pl.Count - 1]];
                }
                if (i >= 8) continue;
                if (cur) _mainCount++; else _mainCount--;
                if (v.gameResult) continue;

                if (_mainCount < 8)
                {
                    if (!v.isSmashing) continue;
                    v.DelebiSmash.sprite = v.DelebiSmash.image.sprite
                        = DelebiBundleManager.Instance.DelebiSmash;
                    v.Delebi.enable = 1; v.leftHand.enable = 1;
                    v.rightHand.enable = 1; v.DelebiSmash.enable = 0;
                    v.winkOn = false; v.isSmashing = false;
                }
                else if (!v.isSmashing)
                {
                    v.Delebi.enable = 0; v.leftHand.enable = 0;
                    v.rightHand.enable = 0; v.DelebiSmash.enable = 1;
                    v.isSmashing = true;
                }
            }
        }
        #endregion

        #region Character lifecycle (called from Harmony patches on main thread)
        public static void DieYoon()
        {
            var v = Main.KeyViewer; if (v == null || v.gameResult) return;
            v.gameResult = true; v.isSmashing = false; v.winkOn = false;
            v.YoonClear.sprite = v.YoonClear.image.sprite = YoonBundleManager.Instance.YoonDie;
            v.YoonClear.enable = 1; v.Yoon.enable = 0;
            v.Table.enable = Main.setting.Current.HideDesk ? (sbyte)0 : (sbyte)1;
            v.leftHand.enable = 1; v.rightHand.enable = 1;
            v.leftLeg.enable = Main.setting.Current.HideFeet ? (sbyte)0 : (sbyte)1;
            v.rightLeg.enable = Main.setting.Current.HideFeet ? (sbyte)0 : (sbyte)1;
            v.FeetKeyboard.enable = Main.setting.Current.HideFeet ? (sbyte)0 : Main.setting.Current.HideFeetKeyboard ? (sbyte)0 : (sbyte)1;
            v.YoonSmash.enable = 0;
        }

        public static void ClearYoon()
        {
            var v = Main.KeyViewer; if (v == null || v.gameResult) return;
            v.gameResult = true; v.isSmashing = false; v.winkOn = false;
            v.Table.enable = Main.setting.Current.HideDesk ? (sbyte)0 : (sbyte)1;
            v.Yoon.enable = 0; v.leftHand.enable = 0; v.rightHand.enable = 0;
            v.leftLeg.enable = 0; v.rightLeg.enable = 0;
            v.YoonSmash.enable = 0;
            v.YoonClear.sprite = v.YoonClear.image.sprite
                = Main.setting.Current.HideFeet ? YoonBundleManager.Instance.YoonClearNoLeg : YoonBundleManager.Instance.YoonClear;
            v.FeetKeyboard.enable = Main.setting.Current.HideFeet ? (sbyte)0 : Main.setting.Current.HideFeetKeyboard ? (sbyte)0 : (sbyte)1;
            v.YoonClear.enable = 1;
        }

        public static void ResetPatchYoon()
        {
            var v = Main.KeyViewer; if (v == null) return;
            v.Table.enable = Main.setting.Current.HideDesk ? (sbyte)0 : (sbyte)1;
            if (!v.gameResult)
                v.Yoon.sprite = v.Yoon.image.sprite = v.isNervous
                    ? YoonBundleManager.Instance.YoonNervous : YoonBundleManager.Instance.YoonIdle;
            v.Yoon.enable = 1; v.leftHand.enable = 1; v.rightHand.enable = 1;
            v.leftLeg.enable = Main.setting.Current.HideFeet ? (sbyte)0 : (sbyte)1;
            v.rightLeg.enable = Main.setting.Current.HideFeet ? (sbyte)0 : (sbyte)1;
            v.FeetKeyboard.enable = Main.setting.Current.HideFeet ? (sbyte)0 : Main.setting.Current.HideFeetKeyboard ? (sbyte)0 : (sbyte)1;
            if (v.YoonClear.image.enabled && v.gameResult) { v.Yoon.enable = 0; v.YoonClear.enable = 1; }
            else { v.YoonClear.enable = 0; }
            if (v.YoonSmash.image.enabled && v.isSmashing)
            {
                v.Yoon.enable = 0; v.leftHand.enable = 0; v.rightHand.enable = 0; v.YoonSmash.enable
= 1;
            }
            else { v.YoonSmash.enable = 0; v.YoonSmash.image.sprite = YoonBundleManager.Instance.YoonSmash; }
            v.gameResult = false;
        }

        public static void DieLine()
        {
            var v = Main.KeyViewerLine; if (v == null || v.gameResult) return;
            v.gameResult = true; bool head = v.headOn; v.headOn = false; v.winkOn = false;
            v.head.sprite = LineBundleManager.Instance.LineDie;
            if (head) return;
            if (Main.setting.Current.HideDesk) v.mainImage.enable = 0; else v.mainImage.sprite = LineBundleManager.Instance.Table;
            v.leftHand.enable = 0; v.rightHand.enable = 0; v.head.enable = 1;
        }

        public static void ClearLine()
        {
            var v = Main.KeyViewerLine; if (v == null || v.gameResult) return;
            v.gameResult = true; bool head = v.headOn; v.headOn = false; v.winkOn = false;
            v.mainImage.sprite = Main.setting.Current.HideDesk ? LineBundleManager.Instance.LineClear : LineBundleManager.Instance.LineClearTable;
            if (head) { v.mainImage.enable = 1; v.head.enable = 0; }
            else { v.leftHand.enable = 0; v.rightHand.enable = 0; }
        }

        public static void ResetPatchLine()
        {
            var v = Main.KeyViewerLine; if (v == null) return;
            v.headOn = false; v.gameResult = false; v.head.enable = 0;
            v.leftHand.enable = 1; v.rightHand.enable = 1;
            v.mainImage.sprite = Main.setting.Current.HideDesk ? LineBundleManager.Instance.Line : LineBundleManager.Instance.LineTable;
            v.mainImage.enable = 1;
        }

        public static void DieDelebi()
        {
            var v = Main.KeyViewerDelebi;
            if (v == null || v.gameResult) return;
            v.gameResult = true; v.isSmashing = false; v.winkOn = false;
            v.DelebiClear.sprite = v.DelebiClear.image.sprite
                = DelebiBundleManager.Instance.DelebiDie;
            v.DelebiClear.enable = 1; v.Delebi.enable = 0;
            v.leftHand.enable = 1; v.rightHand.enable = 1;
            v.DelebiSmash.enable = 0;
        }

        public static void ClearDelebi()
        {
            var v = Main.KeyViewerDelebi;
            if (v == null || v.gameResult) return;
            v.gameResult = true; v.isSmashing = false; v.winkOn = false;
            v.Delebi.enable = 0; v.leftHand.enable = 0;
            v.rightHand.enable = 0; v.DelebiSmash.enable = 0;
            v.DelebiClear.sprite = v.DelebiClear.image.sprite
                = DelebiBundleManager.Instance.DelebiClear;
            v.DelebiClear.enable = 1;
        }

        public static void ResetPatchDelebi()
        {
            var v = Main.KeyViewerDelebi;
            if (v == null) return;
            if (!v.gameResult)
                v.Delebi.sprite = v.Delebi.image.sprite
                    = DelebiBundleManager.Instance.DelebiIdle;
            v.Delebi.enable = 1; v.leftHand.enable = 1; v.rightHand.enable = 1;
            if (v.DelebiClear.image.enabled && v.gameResult)
                { v.Delebi.enable = 0; v.DelebiClear.enable = 1; }
            else { v.DelebiClear.enable = 0; }
            if (v.DelebiSmash.image.enabled && v.isSmashing)
                { v.Delebi.enable = 0; v.leftHand.enable = 0;
                  v.rightHand.enable = 0; v.DelebiSmash.enable = 1; }
            else { v.DelebiSmash.enable = 0;
                   v.DelebiSmash.image.sprite = DelebiBundleManager.Instance.DelebiSmash; }
            v.gameResult = false;
        }
        #endregion
    }
}