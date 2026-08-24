using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests.Editor
{
    public sealed class MainMenuControllerTests
    {
        private const string MainMenuPrefabPath = "Assets/Resources/UI/MainMenuUI.prefab";

        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(false, false, false)]
        public void ResolveLevelUnlocked_CombinesTestingOverrideAndProgress(
            bool unlockAllLevels,
            bool progressionUnlocked,
            bool expected)
        {
            Type controllerType = Type.GetType("BallGame.UI.MainMenuController, Assembly-CSharp");
            Assert.That(controllerType, Is.Not.Null);

            MethodInfo resolve = controllerType.GetMethod(
                "ResolveLevelUnlocked",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolve, Is.Not.Null);
            Assert.That(resolve.Invoke(null, new object[] { unlockAllLevels, progressionUnlocked }), Is.EqualTo(expected));
        }

        [Test]
        public void MainMenuPrefab_UnlocksAllLevelsForTestingByDefault()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Component controller = prefab.GetComponent("MainMenuController");
            Assert.That(controller, Is.Not.Null);

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty unlockAllLevels = serializedController.FindProperty("unlockAllLevelsForTesting");
            Assert.That(unlockAllLevels, Is.Not.Null);
            Assert.That(unlockAllLevels.boolValue, Is.True);
        }
    }
}
