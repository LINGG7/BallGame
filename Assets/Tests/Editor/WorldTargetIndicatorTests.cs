using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.Tests
{
    public sealed class WorldTargetIndicatorTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void Indicator_ImplementsPointerClickHandler()
        {
            Type indicatorType = GetRuntimeType("BallGame.UI.WorldTargetIndicator");

            Assert.That(typeof(IPointerClickHandler).IsAssignableFrom(indicatorType), Is.True);
        }

        [Test]
        public void GameHud_TargetIndicatorsReceiveRaycasts()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/GameHud.prefab");

            Assert.That(prefab, Is.Not.Null);
            AssertIndicatorReceivesRaycasts(prefab.transform.Find("BallArrow"));
            AssertIndicatorReceivesRaycasts(prefab.transform.Find("GoalArrow"));
        }

        [Test]
        public void VisibleBuildIndicator_ClickSetsClampedSmoothPanTarget()
        {
            Type managerType = GetRuntimeType("BallGame.Core.GameManager");
            Type gameStateType = GetRuntimeType("BallGame.Core.GameState");
            Type controllerType = GetRuntimeType("BallGame.Core.CameraController2D");
            Type boundsType = GetRuntimeType("BallGame.Core.MapBounds2D");
            Type indicatorType = GetRuntimeType("BallGame.UI.WorldTargetIndicator");

            GameObject managerObject = new GameObject("Indicator Test Manager");
            GameObject boundsObject = new GameObject("Indicator Test Bounds");
            GameObject cameraObject = new GameObject("Indicator Test Camera");
            GameObject targetObject = new GameObject("Indicator Test Target");
            GameObject indicatorObject = new GameObject("Indicator Test Arrow", typeof(RectTransform), typeof(Image));

            try
            {
                Component manager = managerObject.AddComponent(managerType);
                SetGameState(manager, managerType, gameStateType, "Build");

                Component mapBounds = boundsObject.AddComponent(boundsType);
                boundsType.GetMethod("SetBounds").Invoke(mapBounds, new object[] { Vector2.zero, new Vector2(20f, 20f) });

                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                Component controller = cameraObject.AddComponent(controllerType);
                SerializedObject controllerSerialized = new SerializedObject(controller);
                controllerSerialized.FindProperty("gameManager").objectReferenceValue = manager;
                controllerSerialized.FindProperty("mapBounds").objectReferenceValue = mapBounds;
                controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

                targetObject.transform.position = new Vector3(100f, 100f, 0f);
                Component indicator = indicatorObject.AddComponent(indicatorType);
                indicatorType.GetMethod("Bind").Invoke(indicator, new object[] { manager, camera, targetObject.transform, null });

                Vector3 cameraPositionBeforeClick = cameraObject.transform.position;
                PointerEventData click = new PointerEventData(null)
                {
                    button = PointerEventData.InputButton.Left
                };
                indicatorType.GetMethod("OnPointerClick").Invoke(indicator, new object[] { click });

                Vector3 panTarget = (Vector3)controllerType.GetField("panTargetPosition", InstanceFlags).GetValue(controller);
                Vector3 expected = (Vector3)boundsType.GetMethod("ClampCameraPosition").Invoke(
                    mapBounds,
                    new object[] { camera, new Vector3(targetObject.transform.position.x, targetObject.transform.position.y, cameraPositionBeforeClick.z) });

                Assert.That(cameraObject.transform.position, Is.EqualTo(cameraPositionBeforeClick), "A smooth focus must not teleport the camera.");
                Assert.That(panTarget.x, Is.EqualTo(expected.x).Within(0.001f));
                Assert.That(panTarget.y, Is.EqualTo(expected.y).Within(0.001f));
                Assert.That(panTarget.z, Is.EqualTo(expected.z).Within(0.001f));

                SetGameState(manager, managerType, gameStateType, "Simulate");
                targetObject.transform.position = Vector3.zero;
                indicatorType.GetMethod("OnPointerClick").Invoke(indicator, new object[] { click });
                Vector3 panTargetAfterSimulateClick = (Vector3)controllerType.GetField("panTargetPosition", InstanceFlags).GetValue(controller);
                Assert.That(panTargetAfterSimulateClick, Is.EqualTo(panTarget), "Non-build clicks must be ignored.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(indicatorObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(boundsObject);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        private static Type GetRuntimeType(string fullName)
        {
            Type type = Type.GetType(fullName + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, $"Could not find runtime type {fullName}.");
            return type;
        }

        private static void AssertIndicatorReceivesRaycasts(Transform indicator)
        {
            Assert.That(indicator, Is.Not.Null);
            Image image = indicator.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.True);
        }

        private static void SetGameState(Component manager, Type managerType, Type gameStateType, string stateName)
        {
            FieldInfo stateField = managerType.GetField("<State>k__BackingField", InstanceFlags);
            Assert.That(stateField, Is.Not.Null);
            stateField.SetValue(manager, Enum.Parse(gameStateType, stateName));
        }
    }
}
