using System.Runtime.InteropServices;
using UnityEngine;

namespace BallGame.LevelDesign
{
    public static class CollaborativeWebGLFileBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void BallGameDownloadText(string filename, string content);
        [DllImport("__Internal")] private static extern void BallGamePickJson(string gameObjectName, string callbackMethod);
#endif

        public static void Download(string filename, string content)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BallGameDownloadText(filename, content);
#else
            GUIUtility.systemCopyBuffer = content;
            Debug.Log("Level JSON copied to clipboard: " + filename);
#endif
        }

        public static void PickJson(string gameObjectName, string callbackMethod)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BallGamePickJson(gameObjectName, callbackMethod);
#else
            Debug.Log("JSON upload is available in the WebGL build. Call ImportJson in Editor tests.");
#endif
        }
    }
}
