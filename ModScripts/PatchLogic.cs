using YoonKeyViewer.Component;

// ====================================================================
// PatchLogic — shared postfix logic for game lifecycle patches.
//
// Pattern from JipperOverlayer: the actual logic lives here, not in
// the patch class. This makes it easy to share or override per version.
// ====================================================================

namespace YoonKeyViewer
{
    public static class PatchLogic
    {
        /// <summary>Handle Fail / Fail2 state → show die animation.</summary>
        public static void OnFail()
        {
            if (Main.setting.Character == CharacterType.Line)
                KeyInputManager.DieLine();
            else
                KeyInputManager.DieYoon();
        }

        /// <summary>Handle Won state → show clear animation (or die if noFail+deaths).</summary>
        public static void OnWon()
        {
            var ctrl = scrController.instance;
            if (ctrl != null && VersionSafe.HasNoFail(ctrl) && VersionSafe.GetDeaths(ctrl) != 0)
            {
                if (Main.setting.Character == CharacterType.Line)
                    KeyInputManager.DieLine();
                else
                    KeyInputManager.DieYoon();
            }
            else
            {
                if (Main.setting.Character == CharacterType.Line)
                    KeyInputManager.ClearLine();
                else
                    KeyInputManager.ClearYoon();
            }
        }

        /// <summary>Handle any other state change → reset viewer.</summary>
        public static void OnStateReset()
        {
            if (Main.KeyViewer) Main.KeyViewer.isNervous = false;
            if (Main.setting.Character == CharacterType.Line)
                KeyInputManager.ResetPatchLine();
            else
                KeyInputManager.ResetPatchYoon();
        }

        /// <summary>Handle wipe-to-black → hide nervous, reset viewer.</summary>
        public static void OnWipeToBlack()
        {
            if (Main.KeyViewer) Main.KeyViewer.isNervous = false;
            if (Main.setting.Character == CharacterType.Line)
                KeyInputManager.ResetPatchLine();
            else
                KeyInputManager.ResetPatchYoon();
        }

        /// <summary>Handle editor ResetScene → reset viewer.</summary>
        public static void OnEditorReset()
        {
            if (Main.KeyViewer) Main.KeyViewer.isNervous = false;
            if (Main.setting.Character == CharacterType.Line)
                KeyInputManager.ResetPatchLine();
            else
                KeyInputManager.ResetPatchYoon();
        }

        /// <summary>Handle editor SwitchToEditMode → reset viewer.</summary>
        public static void OnEditorSwitchMode()
        {
            if (Main.KeyViewer) Main.KeyViewer.isNervous = false;
            if (Main.setting.Character == CharacterType.Line)
                KeyInputManager.ResetPatchLine();
            else
                KeyInputManager.ResetPatchYoon();
        }

        /// <summary>Handle StartLoadingScene → reset viewer.</summary>
        public static void OnStartLoadingScene()
        {
            if (Main.KeyViewer) Main.KeyViewer.isNervous = false;
            if (Main.setting.Character == CharacterType.Line)
                KeyInputManager.ResetPatchLine();
            else
                KeyInputManager.ResetPatchYoon();
        }

        /// <summary>
        /// Handle MoveToNextFloor — update nervous state based on current BPM.
        /// Uses VersionSafe for version-agnostic BPM/speed access.
        /// </summary>
        public static void OnMoveToNextFloor(scrPlanet __instance)
        {
            if (Main.setting.Character != CharacterType.Yoon) return;

            double speed = VersionSafe.GetPlanetSpeed(__instance.controller);
            float pitch = VersionSafe.GetPitch();
            double bpm = VersionSafe.GetBpm() * pitch;

            bool nervous = (bpm * speed) >= Main.setting.NervousBPM;
            Main.KeyViewer.isNervous = nervous;

            Main.KeyViewer.Yoon.sprite = Main.KeyViewer.Yoon.image.sprite =
                nervous ? YoonBundleManager.Instance.YoonNervous : YoonBundleManager.Instance.YoonIdle;
        }
    }
}
