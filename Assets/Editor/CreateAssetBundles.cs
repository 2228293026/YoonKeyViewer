using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles
{
    [MenuItem("Assets/Build Bundles")]
    private static void BuildAllAssetBundles()
    {
        SetBundle("Assets/Prefabs/YoonKeyViewer.prefab", "ykv_assets");
        SetBundle("Assets/Textures2D/Yoon", "ykv_assets", true);
        SetBundle("Assets/Prefabs/LineKeyViewer.prefab", "line");
        SetBundle("Assets/Textures2D/Line", "line", true);
        SetBundle("Assets/Prefabs/DelebiKeyViewer.prefab", "dkv_assets");
        SetBundle("Assets/Textures2D/Delebi", "dkv_assets", true);
        AssetDatabase.Refresh();

        string outDir = "Assets/AssetBundles";
        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

        BuildPipeline.BuildAssetBundles(
            outDir,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows
        );

        foreach (string name in new[] { "ykv_assets", "line", "dkv_assets" })
        {
            string src = Path.Combine(outDir, name);
            string dst = src + ".bundle";
            if (File.Exists(src)) File.Copy(src, dst, true);
        }

        Debug.Log("Bundles built!");
    }

    static void SetBundle(string path, string bundleName, bool recursive = false)
    {
        if (recursive)
        {
            foreach (string f in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                if (f.EndsWith(".meta")) continue;
                AssetImporter.GetAtPath(f.Replace("\\", "/")).assetBundleName = bundleName;
            }
        }
        else
        {
            AssetImporter.GetAtPath(path).assetBundleName = bundleName;
        }
    }
}
