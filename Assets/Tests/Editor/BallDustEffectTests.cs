using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class BallDustEffectTests
    {
        private static Type DustType => Type.GetType("BallGame.Effects.BallDustEffect, Assembly-CSharp");
        private static Type BallType => Type.GetType("BallGame.Core.BallController, Assembly-CSharp");

        [Test]
        public void ImpactParticleCount_UsesThresholdAndClampsAtMaximum()
        {
            MethodInfo calculate = GetPrivateStaticMethod(DustType, "CalculateImpactParticleCount");

            Assert.That(Invoke<int>(calculate, 1.74f, 1.75f, 12f, 5, 16), Is.Zero);
            Assert.That(Invoke<int>(calculate, 1.75f, 1.75f, 12f, 5, 16), Is.EqualTo(5));
            Assert.That(Invoke<int>(calculate, 50f, 1.75f, 12f, 5, 16), Is.EqualTo(16));
        }

        [Test]
        public void RollingEmissionRate_UsesThresholdAndClampsAtMaximum()
        {
            MethodInfo calculate = GetPrivateStaticMethod(DustType, "CalculateEmissionRate");

            Assert.That(Invoke<float>(calculate, 0.74f, 0.75f, 12f, 8f, 34f), Is.Zero);
            Assert.That(Invoke<float>(calculate, 0.75f, 0.75f, 12f, 8f, 34f), Is.EqualTo(8f).Within(0.001f));
            Assert.That(Invoke<float>(calculate, 50f, 0.75f, 12f, 8f, 34f), Is.EqualTo(34f).Within(0.001f));
        }

        [Test]
        public void ContactSelection_PrefersHighestTangentialSpeed()
        {
            MethodInfo select = GetPrivateStaticMethod(BallType, "ShouldReplaceBestContact");

            Assert.That(Invoke<bool>(select, 1f, 10f, false), Is.True);
            Assert.That(Invoke<bool>(select, 9f, 4f, true), Is.True);
            Assert.That(Invoke<bool>(select, 4f, 9f, true), Is.False);
            Assert.That(Invoke<bool>(select, 9f, 9f, true), Is.False);
        }

        [Test]
        public void Validation_ClampsInvalidRanges()
        {
            GameObject gameObject = new GameObject("BallDustEffectTest");
            try
            {
                Component effect = gameObject.AddComponent(DustType);
                SetField(effect, "minimumRollingSpeed", -1f);
                SetField(effect, "minimumImpactSpeed", -2f);
                SetField(effect, "fullIntensitySpeed", 0f);
                SetField(effect, "minimumImpactParticles", 8);
                SetField(effect, "maximumImpactParticles", 2);
                SetField(effect, "minimumEmissionRate", 12f);
                SetField(effect, "maximumEmissionRate", 4f);

                DustType.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(effect, null);

                Assert.That(GetField<float>(effect, "minimumRollingSpeed"), Is.Zero);
                Assert.That(GetField<float>(effect, "minimumImpactSpeed"), Is.Zero);
                Assert.That(GetField<float>(effect, "fullIntensitySpeed"), Is.GreaterThan(0f));
                Assert.That(GetField<int>(effect, "maximumImpactParticles"), Is.EqualTo(8));
                Assert.That(GetField<float>(effect, "maximumEmissionRate"), Is.EqualTo(12f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void BallPrefab_ContainsDustEffectAndMaterial()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Ball.prefab");
            Assert.That(prefab, Is.Not.Null);

            Component effect = prefab.GetComponent(DustType);
            Assert.That(effect, Is.Not.Null);
            Assert.That(GetField<Material>(effect, "dustMaterial"), Is.Not.Null);
        }

        private static MethodInfo GetPrivateStaticMethod(Type type, string name)
        {
            Assert.That(type, Is.Not.Null);
            MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {name} on {type.FullName}");
            return method;
        }

        private static T Invoke<T>(MethodInfo method, params object[] arguments)
        {
            return (T)method.Invoke(null, arguments);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            field.SetValue(target, value);
        }

        private static T GetField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            return (T)field.GetValue(target);
        }
    }
}
