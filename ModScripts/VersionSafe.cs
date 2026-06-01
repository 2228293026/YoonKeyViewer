using System;
using System.Reflection;

// ====================================================================
// VersionSafe — runtime version detection + delegate-bound API access
//
// Pattern from JipperOverlayer: detect game version once at startup,
// cache Func<> delegates for version-sensitive APIs, then use them
// through the static methods below. Zero reflection cost at runtime.
// ====================================================================

namespace YoonKeyViewer
{
    public static class VersionSafe
    {
        public static bool IsInitialized { get; private set; }
        public static bool IsV141OrLater { get; private set; } = true;

        // ── Delegates (one binding per version) ──
        private static Func<scrController, double> _getPlanetSpeed;
        private static Func<double> _getBpm;
        private static Func<float> _getPitch;
        private static Func<scrController, int> _getDeaths;
        private static Func<scrController, bool> _getNoFail;

        /// <summary>
        /// Call once at mod startup. Detects the running game version and
        /// binds all delegates to the appropriate access path.
        /// </summary>
        public static void Setup()
        {
            if (IsInitialized) return;
            IsInitialized = true;

            IsV141OrLater = DetectApiVersion();
            Main.Logger?.Log($"[YoonKeyViewer] API version: {(IsV141OrLater ? "v141+" : "v136")}");

            if (IsV141OrLater)
                BindV141Delegates();
            else
                BindV136Delegates();
        }

        // ── Detection ──

        /// <summary>
        /// Detect by checking for types/properties introduced in v141+.
        /// scrMarginTracker and ADOBase.playerManager are the key indicators.
        /// </summary>
        private static bool DetectApiVersion()
        {
            try
            {
                // Check both indicators to avoid false negatives
                bool hasMarginTracker = Type.GetType("scrMarginTracker, Assembly-CSharp", false) != null;
                bool hasPlayerManager = typeof(ADOBase).GetProperty("playerManager",
                    BindingFlags.Public | BindingFlags.Static) != null;
                return hasMarginTracker && hasPlayerManager;
            }
            catch
            {
                return false;
            }
        }

        // ── v141+ bindings — direct member access, zero overhead ──

        private static void BindV141Delegates()
        {
            _getPlanetSpeed = ctrl =>
            {
                if (ctrl?.playerOne?.planetarySystem != null)
                    return ctrl.playerOne.planetarySystem.speed;
                return 1.0;
            };

            _getBpm = () =>
            {
                var cond = ADOBase.conductor;
                if (cond == null) return 120.0;
                return cond.bpm;
            };

            _getPitch = () =>
            {
                var cond = ADOBase.conductor;
                if (cond?.song == null) return 1f;
                return cond.song.pitch;
            };

            _getDeaths = ctrl =>
            {
                if (ctrl?.playerOne?.marginTracker == null) return 0;
                return ctrl.playerOne.marginTracker.GetDeaths();
            };

            _getNoFail = ctrl =>
            {
                if (ctrl == null) return false;
                return ctrl.noFail;
            };
        }

        // ── v136 bindings — reflection, only uses FieldInfo.GetValue ──

        private static void BindV136Delegates()
        {
            var ctrlType = typeof(scrController);

            // controller.speed (field)
            var speedField = ctrlType.GetField("speed", BindingFlags.Public | BindingFlags.Instance);
            _getPlanetSpeed = ctrl =>
            {
                if (ctrl == null) return 1.0;
                var val = speedField?.GetValue(ctrl);
                if (val is double d) return d;
                if (val is float f) return f;
                return 1.0;
            };

            // ADOBase.conductor (static property => scrConductor.instance)
            var conductorProp = typeof(ADOBase).GetProperty("conductor", BindingFlags.Public | BindingFlags.Static);
            var conductorType = conductorProp?.PropertyType;

            // conductor.bpm (field)
            var bpmField = conductorType?.GetField("bpm", BindingFlags.Public | BindingFlags.Instance);

            // conductor.song (field) -> song.pitch (property on AudioSource)
            var songField = conductorType?.GetField("song", BindingFlags.Public | BindingFlags.Instance);
            var songType = songField?.FieldType;
            var pitchMember = songType?.GetField("pitch", BindingFlags.Public | BindingFlags.Instance)
                ?? (MemberInfo)songType?.GetProperty("pitch", BindingFlags.Public | BindingFlags.Instance);

            _getBpm = () =>
            {
                if (conductorProp == null) return 120.0;
                var cond = conductorProp.GetValue(null, null);
                if (cond == null) return 120.0;
                var val = bpmField?.GetValue(cond);
                if (val is float f) return f;
                if (val is double d) return d;
                return 120.0;
            };

            _getPitch = () =>
            {
                if (conductorProp == null) return 1f;
                var cond = conductorProp.GetValue(null, null);
                if (cond == null) return 1f;
                var song = songField?.GetValue(cond);
                if (song == null) return 1f;
                var val = pitchMember is FieldInfo pf
                    ? pf.GetValue(song) : pitchMember is System.Reflection.PropertyInfo pp
                    ? pp.GetValue(song, null) : null;
                if (val is float f) return f;
                return 1f;
            };

            // v136 has mistakesManager as a field on scrController
            var mmField = ctrlType.GetField("mistakesManager", BindingFlags.Public | BindingFlags.Instance);
            var mmType = mmField?.FieldType;
            var getDeathsMethod = mmType?.GetMethod("GetDeaths", BindingFlags.Public | BindingFlags.Instance);
            var noFailField = ctrlType.GetField("noFail", BindingFlags.Public | BindingFlags.Instance);

            _getDeaths = ctrl =>
            {
                if (ctrl == null) return 0;
                var mm = mmField?.GetValue(ctrl);
                if (mm == null) return 0;
                var val = getDeathsMethod?.Invoke(mm, null);
                return val is int i ? i : 0;
            };

            _getNoFail = ctrl =>
            {
                if (ctrl == null) return false;
                var val = noFailField?.GetValue(ctrl);
                return val is bool b && b;
            };
        }

        // ── Public API — always call these, never the raw fields ──

        /// <summary>Gets the current planet speed (double).</summary>
        public static double GetPlanetSpeed(scrController ctrl) => _getPlanetSpeed?.Invoke(ctrl) ?? 1.0;

        /// <summary>Gets the current BPM from the conductor.</summary>
        public static double GetBpm() => _getBpm?.Invoke() ?? 120.0;

        /// <summary>Gets the current song pitch from the conductor.</summary>
        public static float GetPitch() => _getPitch?.Invoke() ?? 1f;

        /// <summary>Gets the death count from mistakesManager/marginTracker.</summary>
        public static int GetDeaths(scrController ctrl) => _getDeaths?.Invoke(ctrl) ?? 0;

        /// <summary>Checks whether noFail mode is active.</summary>
        public static bool HasNoFail(scrController ctrl) => _getNoFail?.Invoke(ctrl) ?? false;
    }
}
