using HarmonyLib;
using MonsterLove.StateMachine;
using System;
using YoonKeyViewer.Component;

// ====================================================================
// GameLifecyclePatches — Harmony patches for game lifecycle events.
//
// Replaces the old monolithic ResultHandler.cs.
// All patches target classes that exist in BOTH v136 and v141+.
// Version-specific data access is handled via VersionSafe delegates.
// ====================================================================

namespace YoonKeyViewer
{
    /// <summary>
    /// Registers all lifecycle patches with PatchManager.
    /// Call from Main.cs after VersionSafe.Setup().
    /// </summary>
    public static class GameLifecyclePatches
    {
        public static void RegisterAll()
        {
            PatchManager.RegisterAll(() => true,
                typeof(ChangeStatePatch),
                typeof(WipeToBlackPatch),
                typeof(EditorResetScenePatch),
                typeof(EditorSwitchToEditModePatch),
                typeof(MoveToNextFloorPatch),
                typeof(StartLoadingScenePatch)
            );
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // StateMachine — handles Fail / Won / reset
    // ─────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(StateBehaviour), nameof(StateBehaviour.ChangeState), new Type[] { typeof(Enum) })]
    internal static class ChangeStatePatch
    {
        static void Postfix(Enum newState)
        {
            switch ((States)newState)
            {
                case States.Fail:
                case States.Fail2:
                    PatchLogic.OnFail();
                    break;
                case States.Won:
                    PatchLogic.OnWon();
                    break;
                default:
                    PatchLogic.OnStateReset();
                    break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Scene transitions
    // ─────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(scrUIController), "WipeToBlack")]
    internal static class WipeToBlackPatch
    {
        static void Postfix() => PatchLogic.OnWipeToBlack();
    }

    [HarmonyPatch(typeof(scnEditor), "ResetScene")]
    internal static class EditorResetScenePatch
    {
        static void Postfix() => PatchLogic.OnEditorReset();
    }

    [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
    internal static class EditorSwitchToEditModePatch
    {
        static void Postfix() => PatchLogic.OnEditorSwitchMode();
    }

    [HarmonyPatch(typeof(scrController), "StartLoadingScene")]
    internal static class StartLoadingScenePatch
    {
        static void Postfix() => PatchLogic.OnStartLoadingScene();
    }

    // ─────────────────────────────────────────────────────────────────
    // Floor progression — BPM-based nervous detection
    // ─────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(scrPlanet), "MoveToNextFloor")]
    internal static class MoveToNextFloorPatch
    {
        static void Postfix(scrPlanet __instance) => PatchLogic.OnMoveToNextFloor(__instance);
    }
}
