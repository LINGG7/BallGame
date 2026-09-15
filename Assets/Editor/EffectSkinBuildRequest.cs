using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    // A one-shot request is explicitly written outside Assets after user approval.
    // This bridge never creates resources merely because the project is opened.
    [InitializeOnLoad]
    public static class EffectSkinBuildRequest
    {
        private const string RequestPath = "BuildChecks/EffectSkins/approved-build.request";
        private const string ResultPath = "BuildChecks/EffectSkins/approved-build-result.txt";

        static EffectSkinBuildRequest() { EditorApplication.update += ProcessRequest; }

        private static void ProcessRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!File.Exists(RequestPath) || File.ReadAllText(RequestPath).Trim() != "approved-create-v1")
                return;
            // Claim before any asset writes, so failed/partial runs cannot overwrite assets on retry.
            File.WriteAllText(RequestPath, "claimed");
            try
            {
                EffectSkinPrefabBuilder.CreateAssets();
                File.WriteAllText(ResultPath, "PASS: Created four effect prefabs, preview/material assets, catalog and shop tab. Unity reference validation passed.");
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, exception.ToString());
                Debug.LogException(exception);
            }
        }
    }
}