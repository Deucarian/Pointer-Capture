using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Controls = Deucarian.Editor.DeucarianEditorWorkspaceControls;
using Manager = Deucarian.PointerCapture.Editor.DeucarianPointerCaptureManagerWindow;

namespace Deucarian.PointerCapture.Editor
{
    internal sealed class PointerCapturePage
    {
        private readonly DeucarianEditorWorkspace workspace;
        private readonly List<DeucarianEditorSerializedForm> bindings = new List<DeucarianEditorSerializedForm>();
        private readonly List<DeucarianEditorWorkspaceForm> live = new List<DeucarianEditorWorkspaceForm>();
        private DeucarianPointerCaptureController controller;
        private DeucarianPointerCaptureController testController;
        private double releaseAt;
        private bool wasPlaying;
        private bool canTest;
        private double nextAvailabilityCheck;
        private Button test;
        public IDeucarianEditorPage Page { get; }

        internal PointerCapturePage()
        {
            controller = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponent<DeucarianPointerCaptureController>() : null;
            var root = new VisualElement();
            workspace = new DeucarianEditorWorkspace(root, Application.productName);
            workspace.Title.text = "Pointer capture";
            workspace.Subtitle.text = "Keep cursor behavior predictable.";
            DeucarianEditorWorkspaceNavigation.Populate(workspace, DeucarianToolIds.PointerCapture);
            root.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) ReleaseTest(); });
            Page = new DeucarianEditorPage(root, activate: _ => Update(), deactivate: ReleaseTest,
                update: _ => Update(), dispose: Dispose);
            Render();
        }

        private void Render()
        {
            ClearBindings();
            live.Clear();
            workspace.Content.Clear();
            wasPlaying = EditorApplication.isPlaying;
            var scroll = Controls.Scroll("pointer-settings");
            workspace.Content.Add(scroll);
            var context = new DeucarianEditorFeatureSection("pointer-context", "Pointer settings",
                "Configure how pointer capture behaves for this project.", DeucarianEditorIconIds.Document);
            context.Root.AddToClassList("dw-feature-context");
            scroll.Add(context.Root);
            var card = new DeucarianEditorFeatureSection("capture-policy", "Capture policy",
                "Your input integration requests capture. This policy controls whether it is allowed.",
                DeucarianEditorIconIds.Pointer);
            Controls.Show(card.Description, false);
            card.Root.AddToClassList("dw-feature-settings");
            scroll.Add(card.Root);
            VisualElement recovery = null;
            var picker = new ObjectField { name = "pointer-controller", objectType = typeof(DeucarianPointerCaptureController),
                allowSceneObjects = true, value = controller };
            picker.RegisterValueChangedCallback(evt =>
            {
                ReleaseTest();
                controller = evt.newValue as DeucarianPointerCaptureController;
                Render();
            });
            context.Details.Add(Controls.Field("Controller", picker));
            context.SetState(true);
            if (controller == null)
            {
                card.Details.Add(Controls.Label("Select a controller to configure capture and test its live state.", "dw-muted"));
                var add = Controls.Button("Add to selected object", () =>
                {
                    if (Selection.activeGameObject == null) return;
                    controller = Undo.AddComponent<DeucarianPointerCaptureController>(Selection.activeGameObject);
                    Render();
                }, true);
                add.SetEnabled(Selection.activeGameObject != null);
                card.Actions.Add(add);
            }
            else
            {
                var form = Bind(card.Details, controller);
                form.Property("allowCapture", "Allow capture");
                form.Property("releasePolicy", "Release on");
                form.Property("hideCursor", "Hide cursor");
                card.Details.Add(Controls.Divider());
                var status = Live(card.Details);
                status.ReadOnly("pointer-state", "Current state", () => controller == null ? "No controller"
                    : EditorApplication.isPlaying ? controller.State.ToString() : "Start Play Mode to test");
                var advanced = new Foldout { text = "Recovery and diagnostics", value = false };
                advanced.AddToClassList("dw-foldout");
                recovery = advanced;
                Live(advanced).ReadOnly("pointer-owner", "Owner", () => controller == null ? "None"
                    : Empty(controller.GetDiagnosticsSnapshot().Owner));
                Bind(advanced, controller).Remaining("allowCapture", "releasePolicy", "hideCursor");
                Live(advanced).ReadOnly("pointer-last-reason", "Last release", () => controller == null ? "None" : controller.LastReleaseReason.ToString());
                Live(advanced).ReadOnly("pointer-last-message", "Details", () => controller == null ? "No controller"
                    : controller.DiagnosticsEnabled ? controller.LastMessage : "Detailed diagnostics are off");
                var runtime = Live(advanced).Toggle("pointer-runtime-gate", "Runtime allowed",
                    () => controller != null && controller.RuntimeCaptureAllowed,
                    value => { if (controller != null && EditorApplication.isPlaying) controller.SetRuntimeCaptureAllowed(value); });
                runtime.SetEnabled(EditorApplication.isPlaying);
                test = Controls.IconButton("Test capture", DeucarianEditorIconIds.Play, Test, DeucarianEditorButtonRole.Primary);
                test.tooltip = "In Play Mode, capture for three seconds. Escape or leaving this page also releases this test.";
                card.Actions.Add(test);
                card.Actions.Add(Controls.Button("Restore defaults", Reset));
            }
            BuildProjectSettings(scroll, recovery);
            Update();
        }

        private void BuildProjectSettings(VisualElement parent, VisualElement recovery)
        {
            var details = new Foldout { name = "pointer-platform-details", text = "Platform details", value = false };
            details.AddToClassList("dw-foldout");
            details.AddToClassList("dw-foldout-panel");
            details.AddToClassList("dw-foldout-followup");
            parent.Add(details);
            if (recovery != null) details.Add(recovery);
            var paths = Manager.FindSettingsAssetPaths();
            var settings = AssetDatabase.LoadAssetAtPath<DeucarianPointerCaptureProjectSettings>(Manager.CanonicalSettingsAssetPath)
                ?? Manager.LoadFirstSettings(paths);
            var status = Live(details);
            status.ReadOnly("pointer-platform", "Platform", () => Manager.GetCurrentPlatform().ToString());
            status.ReadOnly("pointer-project-policy", "Runtime policy", () => DeucarianPointerCaptureProjectSettings
                .IsCurrentProjectAllowed(Manager.GetCurrentPlatform()) ? "Capture allowed" : "Capture blocked");
            if (settings != null)
            {
                Bind(details, settings).Remaining();
                details.RegisterCallback<SerializedPropertyChangeEvent>(_ => DeucarianPointerCaptureProjectSettings.Reload());
                details.Add(Controls.Button("Select settings asset", () => { Selection.activeObject = settings; EditorGUIUtility.PingObject(settings); }));
            }
            string state = paths.Count == 0 ? "Using package defaults. No project settings asset."
                : paths.Count > 1 ? "Multiple settings assets found. Keep one at the runtime path."
                : paths[0] == Manager.CanonicalSettingsAssetPath ? "One runtime settings asset configured."
                : "Settings are outside the runtime Resources path.";
            details.Add(Controls.Label(state, "dw-muted"));
            if (paths.Count > 1) foreach (var path in paths) details.Add(Controls.Label(path, "dw-muted"));
            if (paths.Count == 0) details.Add(Controls.Button("Create project settings", () => { Manager.CreateProjectSettings(); Render(); }));
            else if (paths.Count == 1 && paths[0] != Manager.CanonicalSettingsAssetPath)
                details.Add(Controls.Button("Move to runtime path", () => { Manager.MoveSettingsToCanonicalPath(paths[0]); Render(); }));
            bool bridge = AssetDatabase.FindAssets("DeucarianPointerCapture t:DefaultAsset").Length > 0;
            details.Add(Controls.Label(bridge ? "WebGL pointer-lock bridge available" : "WebGL pointer-lock bridge not found", "dw-muted"));
            if (!EditorApplication.isPlaying) return;
            var controllers = Object.FindObjectsByType<DeucarianPointerCaptureController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var item in controllers)
            {
                var row = Live(details);
                row.ReadOnly(null, item.name, () => item == null ? "Removed" : item.State + " · " + Empty(item.GetDiagnosticsSnapshot().Owner));
                row.Toggle(null, "Runtime allowed", () => item != null && item.RuntimeCaptureAllowed,
                    value => { if (item != null) item.SetRuntimeCaptureAllowed(value); });
            }
        }

        private void Test()
        {
            if (testController != null) { ReleaseTest(); return; }
            if (!CanTest()) return;
            testController = controller;
            releaseAt = EditorApplication.timeSinceStartup + 3;
            if (!testController.RequestCapture(this)) ReleaseTest();
        }

        private bool CanTest()
        {
            if (!EditorApplication.isPlaying || controller == null || !controller.isActiveAndEnabled || !controller.CanRequestCapture) return false;
            foreach (var item in Object.FindObjectsByType<DeucarianPointerCaptureController>(FindObjectsSortMode.None))
                if (item.State == DeucarianPointerCaptureState.Active || item.State == DeucarianPointerCaptureState.Requested) return false;
            return true;
        }

        private void Update()
        {
            if (!ReferenceEquals(controller, null) && controller == null) { controller = null; Render(); return; }
            if (wasPlaying != EditorApplication.isPlaying) { ReleaseTest(); Render(); return; }
            if (testController != null && EditorApplication.timeSinceStartup >= releaseAt) ReleaseTest();
            if (EditorApplication.timeSinceStartup >= nextAvailabilityCheck)
            {
                nextAvailabilityCheck = EditorApplication.timeSinceStartup + .5;
                canTest = CanTest();
            }
            foreach (var form in live) form.Refresh();
            if (test != null)
            {
                test.SetEnabled(testController != null || canTest);
                var caption = test.Q<Label>();
                if (caption != null) caption.text = testController != null ? "Release test" : "Test capture";
                test.tooltip = testController != null ? "This test releases automatically after three seconds." : "Test in Play Mode when no controller owns the pointer.";
            }
        }

        private void ReleaseTest()
        {
            if (testController != null) testController.ReleaseCapture(this);
            testController = null;
        }

        private void Reset()
        {
            if (controller == null) return;
            using (var value = new SerializedObject(controller))
            {
                foreach (string name in new[] { "allowCapture", "hideCursor", "restorePointerPositionOnRelease", "requireNeutralInputBeforeRearming" })
                    value.FindProperty(name).boolValue = true;
                value.FindProperty("releasePolicy").intValue = (int)DeucarianPointerCaptureReleasePolicy.All;
                value.ApplyModifiedProperties();
            }
            Render();
        }

        private DeucarianEditorSerializedForm Bind(VisualElement root, Object target)
        { var form = new DeucarianEditorSerializedForm(root, target); bindings.Add(form); return form; }
        private DeucarianEditorWorkspaceForm Live(VisualElement root)
        { var form = new DeucarianEditorWorkspaceForm(root); live.Add(form); return form; }
        private static string Empty(string value) => string.IsNullOrEmpty(value) ? "None" : value;
        private void ClearBindings() { foreach (var binding in bindings) binding.Dispose(); bindings.Clear(); test = null; }
        private void Dispose() { ReleaseTest(); ClearBindings(); workspace.Dispose(); }
    }
}
