using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.PointerCapture.Editor
{
    [InitializeOnLoad]
    internal static class PointerCaptureControlCenterRegistration
    {
        private const string PackageId = "com.deucarian.pointer-capture";
        private static readonly IDisposable ToolRegistration;
        private static readonly IDisposable CardRegistration;

        static PointerCaptureControlCenterRegistration()
        {
            ToolRegistration = DeucarianToolRegistry.Register(
                new DeucarianToolDescriptor(
                    DeucarianToolIds.PointerCapture,
                    "Pointer Capture",
                    "Configure cross-platform pointer-lock policy and lifecycle.",
                    DeucarianControlCenterArea.Experience,
                    DeucarianPointerCaptureManagerWindow.OpenWindow,
                    PackageId,
                    searchTerms: new[] { "pointer", "cursor", "lock", "webgl" },
                    order: 120, createPage: DeucarianPointerCaptureManagerWindow.CreatePage));

            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(
                new PointerCaptureCardProvider());
        }

        private sealed class PointerCaptureCardProvider :
            IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterCard> Capture(
                DeucarianControlCenterContext context)
            {
                DeucarianPointerCaptureProjectSettings settings =
                    AssetDatabase.LoadAssetAtPath<
                        DeucarianPointerCaptureProjectSettings>(
                        DeucarianPointerCaptureManagerWindow
                            .CanonicalSettingsAssetPath);
                bool configured = settings != null;
                DeucarianPointerCapturePlatform platform = CurrentPlatform();
                bool policyAllowed = settings == null
                    ? platform != DeucarianPointerCapturePlatform.Unsupported
                    : settings.IsCaptureAllowed(platform);

                return new[]
                {
                    new DeucarianControlCenterCard(
                        PackageId + ".setup",
                        DeucarianControlCenterArea.Experience,
                        "Pointer Capture",
                        "Project pointer-lock policy and package validation.",
                        PackageId,
                        configured && policyAllowed
                            ? DeucarianControlCenterStatus.Success
                            : DeucarianControlCenterStatus.Warning,
                        !policyAllowed
                            ? "Blocked on " + platform
                            : configured
                                ? "Allowed on " + platform
                                : "Using defaults on " + platform,
                        order: 120,
                        details: new[]
                        {
                            configured
                                ? "Project settings asset: configured"
                                : "Project settings asset: missing",
                            "Current platform policy: " +
                            (policyAllowed ? "allowed" : "blocked")
                        },
                        actions: new[]
                        {
                            new DeucarianControlCenterAction(
                                PackageId + ".open",
                                "Open Pointer Capture",
                                DeucarianPointerCaptureManagerWindow.OpenWindow, navigationToolId: DeucarianToolIds.PointerCapture)
                        },
                        searchTerms: new[]
                        {
                            "pointer", "cursor", "lock", "platform", "policy"
                        })
                };
            }

            private static DeucarianPointerCapturePlatform CurrentPlatform()
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
}
