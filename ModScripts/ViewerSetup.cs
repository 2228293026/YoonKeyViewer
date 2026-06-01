using UnityEngine;
using UnityEngine.UI;
using YoonKeyViewer.Component;

namespace YoonKeyViewer
{
    /// <summary>
    /// Wires up custom MonoBehaviour wrappers (Key, AsyncImage, scrYoonKeyViewer, scrLineKeyViewer)
    /// on GameObjects loaded from an AssetBundle. The bundle's prefab stores standard Unity
    /// Image components but cannot carry custom script references (GUID mismatch between DLL and
    /// Unity editor compilation). These helpers create the wrapper hierarchy at runtime.
    /// </summary>
    public static class ViewerSetup
    {
        // ── Yoon ──

        public static scrYoonKeyViewer SetupYoon(GameObject root)
        {
            var viewer = root.AddComponent<scrYoonKeyViewer>();
            Transform sizeObj = root.transform.Find("LocationObject/SizeObject");
            Transform locObj = root.transform.Find("LocationObject");

            viewer.locationTransform = locObj?.GetComponent<RectTransform>();
            viewer.sizeTransform = sizeObj?.GetComponent<RectTransform>();

            // AsyncImage children (visible by default)
            viewer.Table = AddAsyncImage(sizeObj, "Desk");
            viewer.Yoon = AddAsyncImage(sizeObj, "Yoon");
            viewer.YoonSmash = AddHiddenAsyncImage(sizeObj, "YoonSmash");
            viewer.YoonClear = AddHiddenAsyncImage(sizeObj, "YoonClear");
            viewer.leftHand = AddAsyncImage(sizeObj, "LeftHand");
            viewer.rightHand = AddAsyncImage(sizeObj, "RightHand");
            viewer.leftLeg = AddAsyncImage(sizeObj, "LeftLeg");
            viewer.rightLeg = AddAsyncImage(sizeObj, "RightLeg");
            viewer.FeetKeyboard = AddAsyncImage(sizeObj, "FeetPiano");

            // Key children (16 hand keys)
            viewer.keys = new Key[16];
            for (int i = 0; i < 16; i++)
                viewer.keys[i] = AddKey(sizeObj, $"Key{i + 1}");

            // FKey children (4 foot keys)
            viewer.fKeys = new Key[4];
            for (int i = 0; i < 4; i++)
                viewer.fKeys[i] = AddKey(sizeObj, $"FKey{i + 1}");

            viewer.isSmashing = false;
            viewer.winkOn = false;
            viewer.gameResult = false;
            viewer.isNervous = false;

            return viewer;
        }

        // ── Line ──

        public static scrLineKeyViewer SetupLine(GameObject root)
        {
            var viewer = root.AddComponent<scrLineKeyViewer>();
            Transform sizeObj = root.transform.Find("LocationObject/SizeObject");
            Transform locObj = root.transform.Find("LocationObject");

            viewer.locationTransform = locObj?.GetComponent<RectTransform>();
            viewer.sizeTransform = sizeObj?.GetComponent<RectTransform>();

            // AsyncImage children
            viewer.mainImage = AddAsyncImage(sizeObj, "Main");
            viewer.leftHand = AddAsyncImage(sizeObj, "LeftHand");
            viewer.rightHand = AddAsyncImage(sizeObj, "RightHand");
            viewer.head = AddHiddenAsyncImage(sizeObj, "LineHead");

            // Key children (16 keys)
            viewer.keys = new Key[16];
            for (int i = 0; i < 16; i++)
                viewer.keys[i] = AddKey(sizeObj, $"Key{i + 1}");

            viewer.headOn = false;
            viewer.winkOn = false;
            viewer.gameResult = false;

            return viewer;
        }

        // ── Helpers ──

        private static AsyncImage AddAsyncImage(Transform parent, string childName)
        {
            Transform t = parent?.Find(childName);
            if (t == null) return null;

            var img = t.gameObject.AddComponent<AsyncImage>();
            img.image = t.GetComponent<Image>();
            img.enable = 1; // visible by default
            return img;
        }

        private static AsyncImage AddHiddenAsyncImage(Transform parent, string childName)
        {
            Transform t = parent?.Find(childName);
            if (t == null) return null;

            var img = t.gameObject.AddComponent<AsyncImage>();
            img.image = t.GetComponent<Image>();
            img.enable = 0; // hidden by default
            return img;
        }

        private static Key AddKey(Transform parent, string childName)
        {
            Transform t = parent?.Find(childName);
            if (t == null) return null;

            var key = t.gameObject.AddComponent<Key>();
            key.image = t.GetComponent<Image>();
            key.enable = 0; // hidden (unpressed) by default
            return key;
        }
    }
}
