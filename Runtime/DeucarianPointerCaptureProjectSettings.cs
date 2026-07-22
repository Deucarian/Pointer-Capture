using UnityEngine;

namespace Deucarian.PointerCapture
{
    [CreateAssetMenu(
        fileName = "PointerCaptureSettings",
        menuName = "Deucarian/Interaction/Pointer Capture Settings")]
    public sealed class DeucarianPointerCaptureProjectSettings : ScriptableObject
    {
        public const string ResourcesPath = "Deucarian/PointerCaptureSettings";

        [SerializeField] private bool enableCapture = true;
        [SerializeField] private bool allowInEditor = true;
        [SerializeField] private bool allowInStandalone = true;
        [SerializeField] private bool allowInWebGL = true;
        [SerializeField] private bool diagnosticsEnabled = true;

        private static bool loadAttempted;
        private static DeucarianPointerCaptureProjectSettings cachedSettings;

        public bool EnableCapture => enableCapture;

        public bool AllowInEditor => allowInEditor;

        public bool AllowInStandalone => allowInStandalone;

        public bool AllowInWebGL => allowInWebGL;

        public bool DiagnosticsEnabled => diagnosticsEnabled;

        public static DeucarianPointerCaptureProjectSettings Load()
        {
            if (!loadAttempted)
            {
                cachedSettings = Resources.Load<DeucarianPointerCaptureProjectSettings>(ResourcesPath);
                loadAttempted = true;
            }

            return cachedSettings;
        }

        public static void Reload()
        {
            loadAttempted = false;
            cachedSettings = null;
        }

        public bool IsCaptureAllowed(DeucarianPointerCapturePlatform platform)
        {
            if (!enableCapture)
            {
                return false;
            }

            switch (platform)
            {
                case DeucarianPointerCapturePlatform.Editor:
                    return allowInEditor;
                case DeucarianPointerCapturePlatform.Standalone:
                    return allowInStandalone;
                case DeucarianPointerCapturePlatform.WebGL:
                    return allowInWebGL;
                default:
                    return false;
            }
        }

        public static bool IsCurrentProjectAllowed(DeucarianPointerCapturePlatform platform)
        {
            DeucarianPointerCaptureProjectSettings settings = Load();
            return settings == null
                ? platform != DeucarianPointerCapturePlatform.Unsupported
                : settings.IsCaptureAllowed(platform);
        }
    }
}

