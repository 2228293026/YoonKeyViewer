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
            if (c == 8 || c == 9 || c == 12 || c == 13 || c == 19) return c;
            if (c == 27 || c == 32) return c;
            return keyCode switch
            {
                KeyCode.Comma => 188,
                KeyCode.Period => 190,
                KeyCode.Semicolon => 186,
                KeyCode.Slash => 191,
                KeyCode.BackQuote => 192,
                KeyCode.LeftBracket => 219,
                KeyCode.Backslash => 220,
                KeyCode.RightBracket => 221,
                KeyCode.Quote => 222,
                KeyCode.Minus => 189,
                KeyCode.Equals => 187,
                KeyCode.LeftShift => 160,
                KeyCode.RightShift => 161,
                KeyCode.LeftControl => 162,
                KeyCode.RightControl => 163,
                KeyCode.LeftAlt => 164,
                KeyCode.RightAlt => 165,
                KeyCode.Delete => 46,// VK_DELETE
                KeyCode.Insert => 45,// VK_INSERT
                KeyCode.Home => 36,// VK_HOME
                KeyCode.End => 35,// VK_END
                KeyCode.PageUp => 33,// VK_PRIOR
                KeyCode.PageDown => 34,// VK_NEXT
                KeyCode.CapsLock => 20,// VK_CAPITAL
                KeyCode.Numlock => 144,// VK_NUMLOCK
                KeyCode.ScrollLock => 145,// VK_SCROLL
                KeyCode.Print => 44,// VK_SNAPSHOT (PrintScreen)
                KeyCode.Pause => 19,// VK_PAUSE
                KeyCode.Tab => 9,// VK_TAB
                KeyCode.Space => 32,// VK_SPACE
                KeyCode.Return => 13,// VK_RETURN
                KeyCode.Escape => 27,// VK_ESCAPE
                KeyCode.Keypad0 => 96,// VK_NUMPAD0
                KeyCode.Keypad1 => 97,
                KeyCode.Keypad2 => 98,
                KeyCode.Keypad3 => 99,
                KeyCode.Keypad4 => 100,
                KeyCode.Keypad5 => 101,
                KeyCode.Keypad6 => 102,
                KeyCode.Keypad7 => 103,
                KeyCode.Keypad8 => 104,
                KeyCode.Keypad9 => 105,
                KeyCode.KeypadDivide => 111,// VK_DIVIDE
                KeyCode.KeypadMultiply => 106,// VK_MULTIPLY
                KeyCode.KeypadMinus => 109,// VK_SUBTRACT
                KeyCode.KeypadPlus => 107,// VK_ADD
                KeyCode.KeypadPeriod => 110,// VK_DECIMAL
                KeyCode.KeypadEnter => 13,// VK_RETURN
                KeyCode.LeftWindows => 91,// VK_LWIN
                KeyCode.RightWindows => 92,// VK_RWIN
                KeyCode.Menu => 93,// VK_APPS (Application/Menu key)
                KeyCode.F1 => 112,
                KeyCode.F2 => 113,
                KeyCode.F3 => 114,
                KeyCode.F4 => 115,
                KeyCode.F5 => 116,
                KeyCode.F6 => 117,
                KeyCode.F7 => 118,
                KeyCode.F8 => 119,
                KeyCode.F9 => 120,
                KeyCode.F10 => 121,
                KeyCode.F11 => 122,
                KeyCode.F12 => 123,
                _ => (int)keyCode,// 未知键返回原始值
            };
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
            else { v.YoonSmash.enable = 0; v.YoonSmash.sprite = YoonBundleManager.Instance.YoonSmash; }
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
                   v.DelebiSmash.sprite = DelebiBundleManager.Instance.DelebiSmash; }
            v.gameResult = false;
        }
        #endregion
    }
}