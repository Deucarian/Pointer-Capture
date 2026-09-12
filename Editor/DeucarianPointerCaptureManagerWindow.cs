using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.PointerCapture.Editor
{
    public sealed class DeucarianPointerCaptureManagerWindow : EditorWindow
    {
        public const string CanonicalSettingsAssetPath = "Assets/Resources/Deucarian/PointerCaptureSettings.asset";
        private DeucarianEditorPageSession session;
        public static void OpenWindow() => DeucarianEditorToolWindow.Open(DeucarianToolIds.PointerCapture);
        public static IDeucarianEditorPage CreatePage() => new PointerCapturePage().Page;
        private void CreateGUI()
        {
            session?.Dispose();
            session = new DeucarianEditorPageSession(this, DeucarianToolIds.PointerCapture, CreatePage());
        }
        private void OnDisable() { session?.Dispose(); session = null; }

        internal static List<string> FindSettingsAssetPaths()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:DeucarianPointerCaptureProjectSettings");
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            return paths;
        }

        internal static DeucarianPointerCaptureProjectSettings LoadFirstSettings(
            IReadOnlyList<string> paths)
        {
            return paths.Count != 1
                ? null
                : AssetDatabase.LoadAssetAtPath<DeucarianPointerCaptureProjectSettings>(paths[0]);
        }

        internal static void CreateProjectSettings()
        {
            EnsureFolder("Assets/Resources/Deucarian");
            if (AssetDatabase.LoadAssetAtPath<Object>(CanonicalSettingsAssetPath) != null)
            {
                return;
            }

            DeucarianPointerCaptureProjectSettings settings =
                CreateInstance<DeucarianPointerCaptureProjectSettings>();
            AssetDatabase.CreateAsset(settings, CanonicalSettingsAssetPath);
            AssetDatabase.SaveAssets();
            DeucarianPointerCaptureProjectSettings.Reload();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        internal static void MoveSettingsToCanonicalPath(string sourcePath)
        {
            EnsureFolder("Assets/Resources/Deucarian");
            if (AssetDatabase.LoadAssetAtPath<Object>(CanonicalSettingsAssetPath) != null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, CanonicalSettingsAssetPath);
            if (string.IsNullOrEmpty(error))
            {
                AssetDatabase.SaveAssets();
                DeucarianPointerCaptureProjectSettings.Reload();
            }
        }

        internal static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        internal static DeucarianPointerCapturePlatform GetCurrentPlatform()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return DeucarianPointerCapturePlatform.WebGL;
#elif UNITY_EDITOR
            return DeucarianPointerCapturePlatform.Editor;
#elif UNITY_STANDALONE
            return DeucarianPointerCapturePlatform.Standalone;
#else
            return DeucarianPointerCapturePlatform.Unsupported;
#endif
        }

    }
}
