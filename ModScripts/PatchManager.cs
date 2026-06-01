using HarmonyLib;
using System;
using System.Collections.Generic;

// ====================================================================
// PatchManager — centralized Harmony patch lifecycle
//
// Pattern from JipperOverlayer: register patches, apply on enable,
// unpatch on disable. Supports conditional toggles for version-
// specific patches.
// ====================================================================

namespace YoonKeyViewer
{
    public static class PatchManager
    {
        private static Harmony _harmony;
        private static string _harmonyId;
        private static readonly Dictionary<Type, Func<bool>> _registeredPatches = new();
        private static readonly HashSet<Type> _appliedPatches = new();

        /// <summary>
        /// Initialize with the Harmony instance. Call once at mod enable.
        /// </summary>
        public static void Initialize(Harmony harmony)
        {
            _harmony = harmony;
            _harmonyId = harmony.Id;
            _registeredPatches.Clear();
            _appliedPatches.Clear();
        }

        /// <summary>
        /// Register a patch class with an optional toggle.
        /// toggle defaults to () => true (always enabled).
        /// </summary>
        public static void Register(Type patchType, Func<bool> toggle = null)
        {
            _registeredPatches[patchType] = toggle ?? (() => true);
        }

        /// <summary>
        /// Register multiple patch classes with the same toggle.
        /// </summary>
        public static void RegisterAll(Func<bool> toggle, params Type[] patchTypes)
        {
            foreach (var t in patchTypes)
                Register(t, toggle);
        }

        /// <summary>
        /// Apply all registered patches whose toggle returns true.
        /// Safe to call multiple times — already-applied patches are skipped.
        /// </summary>
        public static void ApplyAll()
        {
            if (_harmony == null) return;

            foreach (var kvp in _registeredPatches)
            {
                if (_appliedPatches.Contains(kvp.Key)) continue;
                if (!kvp.Value()) continue;

                try
                {
                    _harmony.CreateClassProcessor(kvp.Key).Patch();
                    _appliedPatches.Add(kvp.Key);
                }
                catch (Exception ex)
                {
                    Main.Logger?.Log($"[PatchManager] Failed to patch {kvp.Key.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Unpatch all patches applied through this manager.
        /// </summary>
        public static void UnpatchAll()
        {
            if (_harmony == null) return;
            _harmony.UnpatchAll(_harmonyId);
            _appliedPatches.Clear();
        }
    }
}
