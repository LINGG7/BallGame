using BallGame.Core;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
	public static class LowestCostModeAssetUtility
	{
		private const string CatalogPath = "Assets/Tools/ToolCatalog.asset";

		private static readonly string[] ToolPaths = new string[9] { "Assets/Tools/Plank.asset", "Assets/Tools/Spring.asset", "Assets/Tools/Fan.asset", "Assets/Tools/Conveyor.asset", "Assets/Tools/Magnet+.asset", "Assets/Tools/Magnet-.asset", "Assets/Tools/Portal.asset", "Assets/Tools/WaterZone.asset", "Assets/Tools/AntiGravity.asset" };

		[MenuItem("Ball Game/Lowest Cost/Refresh Shared Tool Catalog")]
		public static void EnsureConfiguration()
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Expected O, but got Unknown
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Expected O, but got Unknown
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0135: Expected O, but got Unknown
			ToolCatalog val = AssetDatabase.LoadAssetAtPath<ToolCatalog>("Assets/Tools/ToolCatalog.asset");
			if ((UnityEngine.Object)(object)val == (UnityEngine.Object)null)
			{
				val = ScriptableObject.CreateInstance<ToolCatalog>();
				AssetDatabase.CreateAsset((UnityEngine.Object)(object)val, "Assets/Tools/ToolCatalog.asset");
			}
			SerializedObject val2 = new SerializedObject((UnityEngine.Object)(object)val);
			SerializedProperty val3 = val2.FindProperty("tools");
			val3.arraySize = ToolPaths.Length;
			for (int i = 0; i < ToolPaths.Length; i++)
			{
				ToolDefinition val4 = AssetDatabase.LoadAssetAtPath<ToolDefinition>(ToolPaths[i]);
				val3.GetArrayElementAtIndex(i).objectReferenceValue = (UnityEngine.Object)(object)val4;
				if ((UnityEngine.Object)(object)val4 != (UnityEngine.Object)null)
				{
					SerializedObject val5 = new SerializedObject((UnityEngine.Object)(object)val4);
					SerializedProperty val6 = val5.FindProperty("cost");
					if (val6.intValue < 1)
					{
						val6.intValue = 1;
						val5.ApplyModifiedPropertiesWithoutUndo();
					}
				}
			}
			val2.ApplyModifiedPropertiesWithoutUndo();
			string[] array = AssetDatabase.FindAssets("t:LevelDefinition", new string[1] { "Assets/LevelDefinitions" });
			string[] array2 = array;
			foreach (string text in array2)
			{
				string text2 = AssetDatabase.GUIDToAssetPath(text);
				LevelDefinition val7 = AssetDatabase.LoadAssetAtPath<LevelDefinition>(text2);
				if (!((UnityEngine.Object)(object)val7 == (UnityEngine.Object)null))
				{
					SerializedObject val8 = new SerializedObject((UnityEngine.Object)(object)val7);
					SerializedProperty val9 = val8.FindProperty("toolCatalog");
					if (val9.objectReferenceValue == (UnityEngine.Object)null)
					{
						val9.objectReferenceValue = (UnityEngine.Object)(object)val;
						val8.ApplyModifiedPropertiesWithoutUndo();
					}
				}
			}
			AssetDatabase.SaveAssets();
		}
	}
}
