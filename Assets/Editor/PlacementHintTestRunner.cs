using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BallGame.Editor
{
    // Explicit, one-shot local test request. Never saves or edits scenes/prefabs/assets.
    [InitializeOnLoad]
    public static class PlacementHintTestRunner
    {
        private const string Request = "BuildChecks/PlacementHints/run-tests.request";
        private const string Result = "BuildChecks/PlacementHints/unity-tests.log";
        private static TestRunnerApi runner;
        static PlacementHintTestRunner() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (runner != null || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            if (File.ReadAllText(Request).Trim() != "run-placement-groups-v1") return;
            File.WriteAllText(Request, "claimed");
            Run();
        }
        [MenuItem("Ball Game/Placement Hints/Run Placement Tests")]
        public static void Run()
        {
            if (runner != null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                Directory.CreateDirectory("BuildChecks/PlacementHints");
                File.WriteAllText(Result, "Running Unity EditMode placement tests...\n");
                runner = ScriptableObject.CreateInstance<TestRunnerApi>();
                runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter
                {
                    testMode = TestMode.EditMode,
                    testNames = new[] { "BallGame.Tests.Editor.PlacementHintTests", "BallGame.Tests.Editor.PlacementHistoryTests" }
                }));
            }
            catch (Exception error) { File.WriteAllText(Result, error.ToString()); Debug.LogException(error); }
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var text = new StringBuilder();
                text.AppendLine($"Passed: {result.PassCount}; Failed: {result.FailCount}; Skipped: {result.SkipCount}");
                Append(result, text);
                File.WriteAllText(Result, text.ToString());
                if (runner != null) { runner.UnregisterCallbacks(this); UnityEngine.Object.DestroyImmediate(runner); }
                runner = null;
            }
            private static void Append(ITestResultAdaptor result, StringBuilder text)
            {
                text.AppendLine(result.ResultState + " " + result.FullName);
                if (!string.IsNullOrEmpty(result.Message)) text.AppendLine(result.Message);
                if (!string.IsNullOrEmpty(result.StackTrace)) text.AppendLine(result.StackTrace);
                foreach (var child in result.Children) Append(child, text);
            }
        }
    }
}
