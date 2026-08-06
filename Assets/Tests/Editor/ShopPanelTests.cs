using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BallGame.Tests.Editor
{
    public sealed class ShopPanelTests
    {
        private const string ShopPrefabPath = "Assets/Resources/UI/ShopModal.prefab";

        [Test]
        public void Catalog_HasValidUnlockedDefaultSkin()
        {
            UnityEngine.Object catalog = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Resources/Shop/BallSkinCatalog.asset");

            Assert.That(catalog, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(catalog);
            Assert.That(serialized.FindProperty("defaultSkinId").stringValue, Is.EqualTo("wood"));
            SerializedProperty skins = serialized.FindProperty("skins");
            Assert.That(skins.arraySize, Is.GreaterThanOrEqualTo(1));
            SerializedProperty defaultSkin = skins.GetArrayElementAtIndex(0);
            Assert.That(defaultSkin.FindPropertyRelative("id").stringValue, Is.EqualTo("wood"));
            Assert.That(defaultSkin.FindPropertyRelative("initiallyUnlocked").boolValue, Is.True);
            Assert.That(defaultSkin.FindPropertyRelative("previewSprite").objectReferenceValue, Is.Not.Null);
            Assert.That(defaultSkin.FindPropertyRelative("runtimeSprite").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void ShopPrefab_ContainsHorizontalPagedCardArea()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.Find("Panel/CategorySidebar/SkinsTab"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Panel/CategorySidebar/RecommendedTab"), Is.Null);
            Assert.That(prefab.transform.Find("Panel/Content/SkinBanner"), Is.Not.Null);

            Transform cardArea = prefab.transform.Find("Panel/Content/CardArea");
            Assert.That(cardArea, Is.Not.Null);
            Assert.That(cardArea.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(cardArea.GetComponent<EventTrigger>(), Is.Not.Null);
            ScrollRect scroll = cardArea.GetComponent<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.horizontal, Is.True);
            Assert.That(scroll.vertical, Is.False);
            Transform cardContent = cardArea.Find("CardContent");
            Assert.That(cardContent, Is.Not.Null);
            Assert.That(cardContent.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(cardArea.GetComponent<GridLayoutGroup>(), Is.Null);
            Assert.That(cardContent.Find("CardTemplate"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Panel/Content/PrevPageButton"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Panel/Content/NextPageButton"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Panel/SkinDetail/ActionButton"), Is.Not.Null);
        }

        [TestCase(1, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(8, 2)]
        [TestCase(9, 3)]
        public void PageCount_UsesFourCardsPerPage(int cardCount, int expectedPages)
        {
            Type controllerType = Type.GetType("BallGame.UI.ShopPanelController, Assembly-CSharp");
            Assert.That(controllerType, Is.Not.Null);
            MethodInfo calculate = controllerType.GetMethod("CalculatePageCount", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(calculate, Is.Not.Null);
            Assert.That(calculate.Invoke(null, new object[] { cardCount }), Is.EqualTo(expectedPages));
        }

        [TestCase(1, 3, -188f, 1)]
        [TestCase(1, 3, 188f, 1)]
        [TestCase(1, 3, -189f, 2)]
        [TestCase(1, 3, 189f, 0)]
        [TestCase(0, 3, 500f, 0)]
        [TestCase(2, 3, -500f, 2)]
        [TestCase(0, 4, -1000f, 1)]
        [TestCase(3, 4, 1000f, 2)]
        public void SwipeTarget_UsesCardThresholdAndMovesAtMostOnePage(int page, int totalPages, float distance, int expectedPage)
        {
            Type controllerType = Type.GetType("BallGame.UI.ShopPanelController, Assembly-CSharp");
            Assert.That(controllerType, Is.Not.Null);
            MethodInfo resolve = controllerType.GetMethod("ResolveSwipeTargetPage", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolve, Is.Not.Null);
            object result = resolve.Invoke(null, new object[] { page, totalPages, distance, 188f });
            Assert.That(result, Is.EqualTo(expectedPage));
        }

    }
}
