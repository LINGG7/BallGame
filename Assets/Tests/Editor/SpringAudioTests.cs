using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class SpringAudioTests
    {
        private const string SpringPrefabPath = "Assets/Resources/Prefabs/Spring.prefab";
        private const string SpringFreePrefabPath = "Assets/Resources/Prefabs/SpringFree.prefab";

        [TestCase(SpringPrefabPath, "BallGame.Tools.SpringPad")]
        [TestCase(SpringFreePrefabPath, "SpringPadFree")]
        public void SpringPrefab_LoadsSfxAndAnimationCanStart(string prefabPath, string componentTypeName)
        {
            AudioClip springSfx = Resources.Load<AudioClip>("Audio/SFX/Spring");
            Assert.That(springSfx, Is.Not.Null);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                MonoBehaviour spring = instance.GetComponents<MonoBehaviour>()
                    .First(component => component != null && component.GetType().FullName == componentTypeName);
                Type springType = spring.GetType();
                springType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spring, null);

                AudioClip loadedSfx = (AudioClip)springType
                    .GetField("springSfx", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(spring);
                Assert.That(loadedSfx, Is.SameAs(springSfx));
                Assert.DoesNotThrow(() => springType
                    .GetMethod("PlayAnimation", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(spring, null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
