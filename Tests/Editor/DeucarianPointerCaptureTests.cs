using System.IO;
using Deucarian.PointerCapture.Editor;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Deucarian.PointerCapture.Tests
{
    public sealed class DeucarianPointerCaptureTests
    {
        [Test]
        public void FocusLossRequiresNeutralInputThenFreshAction()
        {
            var gate = new DeucarianPointerCaptureRearmGate();

            gate.SetFocus(false);
            Assert.IsFalse(gate.CanProcessInput);

            gate.SetFocus(true);
            gate.Refresh(false, true);
            Assert.IsFalse(gate.CanProcessInput);

            gate.Refresh(true, false);
            Assert.IsFalse(gate.CanProcessInput);

            gate.Refresh(false, true);
            Assert.IsTrue(gate.CanProcessInput);
        }

        [Test]
        public void RearmCanRequireFreshActionWithoutNeutralPhase()
        {
            var gate = new DeucarianPointerCaptureRearmGate();

            gate.BlockUntilNewAction(false);
            gate.Refresh(false, false);
            Assert.IsFalse(gate.CanProcessInput);

            gate.Refresh(false, true);
            Assert.IsTrue(gate.CanProcessInput);
        }

        [Test]
        public void RuntimePolicyRejectsCaptureWithoutTouchingPlatformLock()
        {
            var gameObject = new GameObject("Pointer Capture Test");
            try
            {
                DeucarianPointerCaptureController controller =
                    gameObject.AddComponent<DeucarianPointerCaptureController>();
                controller.SetRuntimeCaptureAllowed(false);

                Assert.IsFalse(controller.RequestCapture(this));
                Assert.AreEqual(DeucarianPointerCaptureState.Rejected, controller.State);
                Assert.AreEqual(
                    DeucarianPointerCaptureReleaseReason.RuntimePolicyChanged,
                    controller.LastReleaseReason);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DefaultProjectSettingsAllowSupportedPlatforms()
        {
            DeucarianPointerCaptureProjectSettings settings =
                ScriptableObject.CreateInstance<DeucarianPointerCaptureProjectSettings>();
            try
            {
                Assert.IsTrue(settings.IsCaptureAllowed(DeucarianPointerCapturePlatform.Editor));
                Assert.IsTrue(settings.IsCaptureAllowed(DeucarianPointerCapturePlatform.Standalone));
                Assert.IsTrue(settings.IsCaptureAllowed(DeucarianPointerCapturePlatform.WebGL));
                Assert.IsFalse(settings.IsCaptureAllowed(DeucarianPointerCapturePlatform.Unsupported));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void PackageHasOneManagementMenuAndIntegratedValidation()
        {
            string root = PackageInfo.FindForAssembly(
                typeof(DeucarianPointerCaptureManagerWindow).Assembly).resolvedPath;
            string source = File.ReadAllText(
                Path.Combine(root, "Editor/DeucarianPointerCaptureManagerWindow.cs"));

            StringAssert.Contains(
                "Tools/Deucarian/Interaction/Pointer Capture",
                source);
            Assert.AreEqual(1, CountOccurrences(source, "[MenuItem("));
            StringAssert.Contains("Validation & Fixes", source);
        }

        [Test]
        public void WebGlBridgeObservesLockErrorVisibilityAndBlur()
        {
            string root = PackageInfo.FindForAssembly(
                typeof(DeucarianPointerCaptureController).Assembly).resolvedPath;
            string source = File.ReadAllText(
                Path.Combine(root, "Runtime/Plugins/WebGL/DeucarianPointerCapture.jslib"));

            StringAssert.Contains("pointerlockchange", source);
            StringAssert.Contains("pointerlockerror", source);
            StringAssert.Contains("visibilitychange", source);
            StringAssert.Contains("window.addEventListener(\"blur\"", source);
            StringAssert.Contains("canvas.requestPointerLock()", source);
            StringAssert.Contains(
                "DeucarianPointerCaptureRequest__deps: [\"$deucarianPointerCaptureInstall\"]",
                source);
            Assert.AreEqual(0, CountOccurrences(source, "function deucarianPointerCaptureInstall"));
        }

        [Test]
        public void WebGlPlatformBridgeDisablesStickyCursorLock()
        {
            string root = PackageInfo.FindForAssembly(
                typeof(DeucarianPointerCaptureController).Assembly).resolvedPath;
            string source = File.ReadAllText(
                Path.Combine(root, "Runtime/DeucarianPointerCapturePlatformBridge.cs"));

            StringAssert.Contains("WebGLInput.stickyCursorLock = false", source);
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }
    }
}
