#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace BallGame.Editor
{
    public static class IOSHapticsBuildPostprocessor
    {
        [PostProcessBuild(100)]
        public static void AddCoreHapticsFramework(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(frameworkTarget, "CoreHaptics.framework", true);
            project.WriteToFile(projectPath);
        }
    }
}
#endif
