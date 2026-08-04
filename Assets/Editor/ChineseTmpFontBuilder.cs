using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BallGame.Editor
{
    public static class ChineseTmpFontBuilder
    {
        private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LXGWWenKai-Light SDF.asset";
        private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LXGWWenKai-Light.ttf";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>
        {
            ".asset",
            ".cs",
            ".csv",
            ".json",
            ".prefab",
            ".txt",
            ".unity"
        };

        [MenuItem("Ball Game/Fonts/Update Chinese TMP Font")]
        public static void UpdateChineseTmpFont()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);

            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("Update Chinese TMP Font", $"Font asset not found:\n{FontAssetPath}", "OK");
                return;
            }

            if (settings == null)
            {
                EditorUtility.DisplayDialog("Update Chinese TMP Font", $"TMP Settings not found:\n{TmpSettingsPath}", "OK");
                return;
            }

            string characters = CollectProjectCharacters();
            if (string.IsNullOrEmpty(characters))
            {
                EditorUtility.DisplayDialog("Update Chinese TMP Font", "No Chinese characters were found in Assets.", "OK");
                return;
            }

            string missingFromAsset = GetCharactersMissingFromFontAsset(fontAsset, characters);
            AtlasPopulationMode previousMode = fontAsset.atlasPopulationMode;
            string missingCharacters = string.Empty;
            bool addedAllCharacters = true;

            if (!string.IsNullOrEmpty(missingFromAsset))
            {
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                if (sourceFont == null)
                {
                    EditorUtility.DisplayDialog("Update Chinese TMP Font", $"Source font not found:\n{SourceFontPath}", "OK");
                    return;
                }

                SerializedObject fontObject = new SerializedObject(fontAsset);
                fontObject.FindProperty("m_SourceFontFile_EditorRef").objectReferenceValue = sourceFont;
                fontObject.FindProperty("m_SourceFontFile").objectReferenceValue = sourceFont;
                fontObject.ApplyModifiedPropertiesWithoutUndo();

                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                addedAllCharacters = fontAsset.TryAddCharacters(missingFromAsset, out missingCharacters);
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            }

            SerializedObject settingsObject = new SerializedObject(settings);
            settingsObject.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
            settingsObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(fontAsset);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!addedAllCharacters)
            {
                Debug.LogError($"Chinese TMP font update missed {missingCharacters.Length} character(s): {missingCharacters}");
                EditorUtility.DisplayDialog(
                    "Update Chinese TMP Font",
                    $"Font updated, but {missingCharacters.Length} character(s) could not be added.\nSee Console for details.",
                    "OK");
                return;
            }

            int addedCount = missingFromAsset.Length - missingCharacters.Length;
            Debug.Log($"Chinese TMP font updated. Scanned {characters.Length} unique Chinese/CJK character(s), added {addedCount}. Previous mode: {previousMode}.");
            EditorUtility.DisplayDialog(
                "Update Chinese TMP Font",
                $"Font updated.\nScanned {characters.Length} unique Chinese/CJK character(s).\nAdded {addedCount} new character(s).",
                "OK");
        }

        [MenuItem("Ball Game/Fonts/Print Chinese Character Set")]
        public static void PrintChineseCharacterSet()
        {
            string characters = CollectProjectCharacters();
            Debug.Log($"Chinese/CJK characters in Assets ({characters.Length}):\n{characters}");
        }

        private static string CollectProjectCharacters()
        {
            SortedSet<char> characters = new SortedSet<char>();

            foreach (string path in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories))
            {
                string normalizedPath = path.Replace('\\', '/');
                if (!ShouldScanFile(normalizedPath))
                {
                    continue;
                }

                AddCharacters(File.ReadAllText(path, Encoding.UTF8), characters);
            }

            StringBuilder builder = new StringBuilder(characters.Count);
            foreach (char character in characters)
            {
                builder.Append(character);
            }

            return builder.ToString();
        }

        private static void AddCharacters(string text, ISet<char> characters)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (ShouldIncludeCharacter(character))
                {
                    characters.Add(character);
                    continue;
                }

                if (character == '\\'
                    && i + 5 < text.Length
                    && text[i + 1] == 'u'
                    && TryParseUnicodeEscape(text, i + 2, out char escapedCharacter))
                {
                    if (ShouldIncludeCharacter(escapedCharacter))
                    {
                        characters.Add(escapedCharacter);
                    }

                    i += 5;
                }
            }
        }

        private static string GetCharactersMissingFromFontAsset(TMP_FontAsset fontAsset, string characters)
        {
            HashSet<uint> existingCharacters = new HashSet<uint>();
            foreach (TMP_Character character in fontAsset.characterTable)
            {
                existingCharacters.Add(character.unicode);
            }

            StringBuilder builder = new StringBuilder();
            foreach (char character in characters)
            {
                if (!existingCharacters.Contains(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static bool ShouldScanFile(string path)
        {
            if (!path.StartsWith("Assets/"))
            {
                return false;
            }

            if (path.StartsWith("Assets/TextMesh Pro/Resources/LineBreaking"))
            {
                return false;
            }

            if (path == FontAssetPath)
            {
                return false;
            }

            return SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }

        private static bool ShouldIncludeCharacter(char character)
        {
            return IsInRange(character, 0x2E80, 0x2EFF)
                || IsInRange(character, 0x3000, 0x303F)
                || IsInRange(character, 0x3400, 0x4DBF)
                || IsInRange(character, 0x4E00, 0x9FFF)
                || IsInRange(character, 0xF900, 0xFAFF)
                || IsInRange(character, 0xFE30, 0xFE4F)
                || IsInRange(character, 0xFF00, 0xFFEF);
        }

        private static bool IsInRange(char character, int start, int end)
        {
            return character >= start && character <= end;
        }

        private static bool TryParseUnicodeEscape(string text, int start, out char character)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                int digit = HexValue(text[start + i]);
                if (digit < 0)
                {
                    character = default;
                    return false;
                }

                value = (value << 4) + digit;
            }

            character = (char)value;
            return true;
        }

        private static int HexValue(char character)
        {
            if (character >= '0' && character <= '9')
            {
                return character - '0';
            }

            if (character >= 'a' && character <= 'f')
            {
                return character - 'a' + 10;
            }

            if (character >= 'A' && character <= 'F')
            {
                return character - 'A' + 10;
            }

            return -1;
        }
    }
}
