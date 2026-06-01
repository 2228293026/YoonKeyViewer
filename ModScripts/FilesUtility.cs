using System;
using System.IO;

namespace YoonKeyViewer
{
    public static class FilesUtility
    {
        public static string GetAssetBundlePath(string targetFileName)
        {
            try
            {
                string[] files = Directory.GetFiles(Main.ModPath, targetFileName, SearchOption.AllDirectories);

                if (files.Length == 0)
                {
                    Main.Logger.Log("File not found.");
                    return null;
                }
                else
                {
                    foreach (string filePath in files)
                    {
                        Main.Logger.Log("Found file at: " + filePath);
                    }

                    return files[0];
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Log("Error: " + ex.Message);
                return null;
            }
        }
    }
}

    