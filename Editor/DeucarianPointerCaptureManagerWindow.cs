using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.PointerCapture.Editor
{
    public sealed class DeucarianPointerCaptureManagerWindow : EditorWindow
    {
        public const string MenuPath = "Tools/Deucarian/Interaction/Pointer Capture";
        public const string CanonicalSettingsAssetPath =
            "Assets/Resources/Deucarian/PointerCaptureSettings.asset";

        private Vector2 scrollPosition;

        [MenuItem(MenuPath, priority = 230)]
        public static void OpenWindow()
        {
            DeucarianPointerCaptureManagerWindow window =
                GetWindow<DeucarianPointerCaptureManagerWindow>("Pointer Capture");
            window.minSize = new Vector2(520f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += RepaintWhilePlaying;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintWhilePlaying;
        }

        private void OnGUI()
        {
            using (DeucarianEditorWorkbenchPanelScope page =
                   DeucarianEditorWorkbenchGUI.BeginSettingsPage(GUILayout.ExpandHeight(true)))
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DeucarianEditorChrome.DrawPackageHeader(
                    "Pointer Capture",
                    "Cross-platform pointer-lock policy, lifecycle, and diagnostics.");

                DrawStatus();
                DrawProjectConfiguration();
                DrawSelectedComponentConfiguration();
                DrawRuntimeStatus();
                DrawValidationAndFixes();

                DeucarianEditorChrome.DrawFooterVersion(
                    "com.deucarian.pointer-capture",
                    "0.1.0");
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawStatus()
        {
            DeucarianEditorChrome.DrawSectionHeader("Status");
            DeucarianEditorChrome.BeginSection();

            DeucarianPointerCapturePlatform platform = GetCurrentPlatform();
            DeucarianPointerCaptureProjectSettings settings =
                DeucarianPointerCaptureProjectSettings.Load();
            bool projectAllowed = settings == null
                ? platform != DeucarianPointerCapturePlatform.Unsupported
                : settings.IsCaptureAllowed(platform);

            DeucarianEditorWorkbenchGUI.DrawReadOnlyRow("Platform", platform.ToString());
            DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                projectAllowed ? "circle-check" : "circle-alert",
                projectAllowed
                    ? "Project policy allows pointer capture on this platform."
                    : "Project policy blocks pointer capture on this platform.",
                projectAllowed ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Warning);

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawProjectConfiguration()
        {
            DeucarianEditorChrome.DrawSectionHeader("Project Configuration");
            DeucarianEditorChrome.BeginSection();

            List<string> settingsPaths = FindSettingsAssetPaths();
            DeucarianPointerCaptureProjectSettings settings = LoadFirstSettings(settingsPaths);
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "No project settings asset exists. Runtime defaults allow capture on Editor, standalone, and WebGL.",
                    MessageType.Info);
                if (GUILayout.Button(
                    "Create Project Settings",
                    DeucarianEditorWorkbenchGUI.PrimaryButtonStyle))
                {
                    CreateProjectSettings();
                }

                DeucarianEditorChrome.EndSection();
                return;
            }

            using (var serializedSettings = new SerializedObject(settings))
            {
                serializedSettings.Update();
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableCapture"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("allowInEditor"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("allowInStandalone"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("allowInWebGL"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("diagnosticsEnabled"));
                if (serializedSettings.ApplyModifiedProperties())
                {
                    DeucarianPointerCaptureProjectSettings.Reload();
                    EditorUtility.SetDirty(settings);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                    "Select Settings Asset",
                    DeucarianEditorWorkbenchGUI.SecondaryButtonStyle))
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }

                if (settingsPaths.Count == 1 &&
                    settingsPaths[0] != CanonicalSettingsAssetPath &&
                    GUILayout.Button(
                        "Move to Runtime Path",
                        DeucarianEditorWorkbenchGUI.SecondaryButtonStyle))
                {
                    MoveSettingsToCanonicalPath(settingsPaths[0]);
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawSelectedComponentConfiguration()
        {
            DeucarianEditorChrome.DrawSectionHeader("Selected Component");
            DeucarianEditorChrome.BeginSection();

            GameObject selected = Selection.activeGameObject;
            DeucarianPointerCaptureController controller =
                selected != null ? selected.GetComponent<DeucarianPointerCaptureController>() : null;
            if (selected == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a scene GameObject to configure its pointer capture policy.",
                    MessageType.Info);
            }
            else if (controller == null)
            {
                EditorGUILayout.HelpBox(
                    selected.name + " does not have a pointer capture controller.",
                    MessageType.Warning);
                if (GUILayout.Button(
                    "Add Pointer Capture Controller",
                    DeucarianEditorWorkbenchGUI.PrimaryButtonStyle))
                {
                    controller = Undo.AddComponent<DeucarianPointerCaptureController>(selected);
                    Selection.activeObject = controller;
                }
            }
            else
            {
                using (var serializedController = new SerializedObject(controller))
                {
                    serializedController.Update();
                    EditorGUILayout.PropertyField(serializedController.FindProperty("allowCapture"));
                    EditorGUILayout.PropertyField(serializedController.FindProperty("hideCursor"));
                    EditorGUILayout.PropertyField(
                        serializedController.FindProperty("requireNeutralInputBeforeRearming"));
                    EditorGUILayout.PropertyField(serializedController.FindProperty("releasePolicy"));
                    serializedController.ApplyModifiedProperties();
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawRuntimeStatus()
        {
            DeucarianEditorChrome.DrawSectionHeader("Runtime");
            DeucarianEditorChrome.BeginSection();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to inspect live controller state and change runtime capture gates.",
                    MessageType.Info);
                DeucarianEditorChrome.EndSection();
                return;
            }

            DeucarianPointerCaptureController[] controllers =
                Object.FindObjectsByType<DeucarianPointerCaptureController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (controllers.Length == 0)
            {
                DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                    "circle-alert",
                    "No active scene contains a pointer capture controller.",
                    DeucarianEditorStatus.Warning);
            }

            foreach (DeucarianPointerCaptureController controller in controllers)
            {
                DeucarianPointerCaptureDiagnosticsSnapshot snapshot =
                    controller.GetDiagnosticsSnapshot();
                using (DeucarianEditorWorkbenchPanelScope panel =
                       DeucarianEditorWorkbenchGUI.BeginPanel(controller.name))
                {
                    DeucarianEditorWorkbenchGUI.DrawReadOnlyRow("State", snapshot.State.ToString());
                    DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                        "Owner",
                        string.IsNullOrEmpty(snapshot.Owner) ? "None" : snapshot.Owner);
                    DeucarianEditorWorkbenchGUI.DrawReadOnlyRow(
                        "Last reason",
                        snapshot.LastReleaseReason.ToString());
                    if (snapshot.DiagnosticsEnabled)
                    {
                        DeucarianEditorWorkbenchGUI.DrawReadOnlyRow("Message", snapshot.Message);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "Detailed runtime diagnostics are disabled in project settings.",
                            MessageType.Info);
                    }

                    bool runtimeAllowed = EditorGUILayout.Toggle(
                        new GUIContent("Runtime allowed"),
                        snapshot.RuntimeAllowed);
                    if (runtimeAllowed != snapshot.RuntimeAllowed)
                    {
                        controller.SetRuntimeCaptureAllowed(runtimeAllowed);
                    }
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawValidationAndFixes()
        {
            DeucarianEditorChrome.DrawSectionHeader("Validation & Fixes");
            DeucarianEditorChrome.BeginSection();

            List<string> settingsPaths = FindSettingsAssetPaths();
            if (settingsPaths.Count == 0)
            {
                DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                    "circle-alert",
                    "Project settings asset is missing; package defaults are in use.",
                    DeucarianEditorStatus.Warning);
                if (GUILayout.Button(
                    "Fix: Create Settings Asset",
                    DeucarianEditorWorkbenchGUI.PrimaryButtonStyle))
                {
                    CreateProjectSettings();
                }
            }
            else if (settingsPaths.Count > 1)
            {
                DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                    "circle-x",
                    "Multiple pointer capture settings assets exist. Keep one canonical asset.",
                    DeucarianEditorStatus.Error);
                foreach (string path in settingsPaths)
                {
                    EditorGUILayout.LabelField(path, DeucarianEditorWorkbenchGUI.MiniLabelStyle);
                }
            }
            else if (settingsPaths[0] != CanonicalSettingsAssetPath)
            {
                DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                    "circle-alert",
                    "Settings are outside the runtime Resources path.",
                    DeucarianEditorStatus.Error);
                if (GUILayout.Button(
                    "Fix: Move Settings to Runtime Path",
                    DeucarianEditorWorkbenchGUI.PrimaryButtonStyle))
                {
                    MoveSettingsToCanonicalPath(settingsPaths[0]);
                }
            }
            else
            {
                DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                    "circle-check",
                    "One canonical runtime settings asset is configured.",
                    DeucarianEditorStatus.Success);
            }

            string[] bridgeGuids = AssetDatabase.FindAssets("DeucarianPointerCapture t:DefaultAsset");
            DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                "plug",
                bridgeGuids.Length > 0
                    ? "WebGL pointer-lock bridge is available."
                    : "WebGL pointer-lock bridge could not be found.",
                bridgeGuids.Length > 0
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Error);

            DeucarianEditorChrome.EndSection();
        }

        private static List<string> FindSettingsAssetPaths()
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

        private static DeucarianPointerCaptureProjectSettings LoadFirstSettings(
            IReadOnlyList<string> paths)
        {
            return paths.Count == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<DeucarianPointerCaptureProjectSettings>(paths[0]);
        }

        private static void CreateProjectSettings()
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

        private static void MoveSettingsToCanonicalPath(string sourcePath)
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

        private static void EnsureFolder(string folderPath)
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

        private static DeucarianPointerCapturePlatform GetCurrentPlatform()
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

        private void RepaintWhilePlaying()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }
    }
}
