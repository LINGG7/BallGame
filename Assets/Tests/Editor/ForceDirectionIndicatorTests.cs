using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace BallGame.Tests
{
    public sealed class ForceDirectionIndicatorTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void Indicator_UsesConfiguredDirectionAndCompensatesParentScale()
        {
            Type managerType = GetRuntimeType("BallGame.Core.GameManager");
            Type stateType = GetRuntimeType("BallGame.Core.GameState");
            Type indicatorType = GetRuntimeType("BallGame.UI.ForceDirectionIndicator");
            GameObject managerObject = new GameObject("Direction Indicator Manager");
            GameObject fanObject = new GameObject("Direction Indicator Fan");
            GameObject springObject = new GameObject("Direction Indicator Spring");

            try
            {
                Component manager = managerObject.AddComponent(managerType);
                SetGameState(manager, managerType, stateType, "Build");

                fanObject.transform.rotation = Quaternion.Euler(0f, 0f, 33f);
                Component fanIndicator = CreateConfiguredIndicator(
                    fanObject,
                    indicatorType,
                    manager,
                    Vector2.right,
                    new Color(0.25f, 0.7f, 1f, 0.9f));
                SpriteRenderer fanArrow = GetArrowRenderer(fanIndicator, indicatorType);
                Assert.That(Vector2.Dot(-fanArrow.transform.up, fanObject.transform.right), Is.GreaterThan(0.999f));

                springObject.transform.localScale = new Vector3(1.2f, 0.25f, 1f);
                Component springIndicator = CreateConfiguredIndicator(
                    springObject,
                    indicatorType,
                    manager,
                    Vector2.up,
                    new Color(0.1f, 0.9f, 0.35f, 0.9f));
                SpriteRenderer springArrow = GetArrowRenderer(springIndicator, indicatorType);
                Assert.That(Vector2.Dot(-springArrow.transform.up, springObject.transform.up), Is.GreaterThan(0.999f));
                Assert.That(springArrow.bounds.size.x, Is.EqualTo(0.22f).Within(0.01f));
                Assert.That(springArrow.bounds.size.y, Is.EqualTo(0.8f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(springObject);
                UnityEngine.Object.DestroyImmediate(fanObject);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void Indicator_IsVisibleOnlyInBuildState()
        {
            Type managerType = GetRuntimeType("BallGame.Core.GameManager");
            Type stateType = GetRuntimeType("BallGame.Core.GameState");
            Type indicatorType = GetRuntimeType("BallGame.UI.ForceDirectionIndicator");
            GameObject managerObject = new GameObject("Direction Indicator State Manager");
            GameObject toolObject = new GameObject("Direction Indicator State Tool");

            try
            {
                Component manager = managerObject.AddComponent(managerType);
                SetGameState(manager, managerType, stateType, "Build");
                Component indicator = CreateConfiguredIndicator(
                    toolObject,
                    indicatorType,
                    manager,
                    Vector2.right,
                    Color.white);
                SpriteRenderer arrow = GetArrowRenderer(indicator, indicatorType);
                Assert.That(arrow.enabled, Is.True);

                SetGameState(manager, managerType, stateType, "Simulate");
                indicatorType.GetMethod("RefreshVisibility", InstanceFlags).Invoke(indicator, null);
                Assert.That(arrow.enabled, Is.False);

                SetGameState(manager, managerType, stateType, "Build");
                indicatorType.GetMethod("RefreshVisibility", InstanceFlags).Invoke(indicator, null);
                Assert.That(arrow.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(toolObject);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [TestCase("BallGame.Tools.FanZone")]
        [TestCase("BallGame.Tools.SpringPad")]
        public void ForceTool_AwakeCreatesOnlyOneIndicator(string forceToolTypeName)
        {
            Type forceToolType = GetRuntimeType(forceToolTypeName);
            Type indicatorType = GetRuntimeType("BallGame.UI.ForceDirectionIndicator");
            GameObject toolObject = new GameObject("Force Tool Indicator Test");

            try
            {
                Component forceTool = toolObject.AddComponent(forceToolType);
                MethodInfo awake = forceToolType.GetMethod("Awake", InstanceFlags);
                awake.Invoke(forceTool, null);
                awake.Invoke(forceTool, null);

                Assert.That(toolObject.GetComponents(indicatorType).Length, Is.EqualTo(1));
                Assert.That(toolObject.transform.Find("ForceDirectionArrow"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(toolObject);
            }
        }

        private static Component CreateConfiguredIndicator(
            GameObject owner,
            Type indicatorType,
            Component manager,
            Vector2 direction,
            Color color)
        {
            Component indicator = owner.AddComponent(indicatorType);
            indicatorType.GetField("gameManager", InstanceFlags).SetValue(indicator, manager);
            indicatorType.GetMethod("Configure").Invoke(indicator, new object[] { direction, color, 0.22f, 0.8f });
            indicatorType.GetMethod("RefreshArrow", InstanceFlags).Invoke(indicator, null);
            indicatorType.GetMethod("RefreshVisibility", InstanceFlags).Invoke(indicator, null);
            return indicator;
        }

        private static SpriteRenderer GetArrowRenderer(Component indicator, Type indicatorType)
        {
            SpriteRenderer renderer = (SpriteRenderer)indicatorType.GetProperty("ArrowRenderer").GetValue(indicator);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            return renderer;
        }

        private static void SetGameState(Component manager, Type managerType, Type stateType, string stateName)
        {
            FieldInfo field = managerType.GetField("<State>k__BackingField", InstanceFlags);
            Assert.That(field, Is.Not.Null);
            field.SetValue(manager, Enum.Parse(stateType, stateName));
        }

        private static Type GetRuntimeType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, fullName);
            return type;
        }
    }
}
