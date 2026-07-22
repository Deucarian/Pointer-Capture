using System.Runtime.InteropServices;
using UnityEngine;

namespace Deucarian.PointerCapture
{
    internal enum DeucarianPointerCapturePlatformStatus
    {
        Idle = 0,
        Requested = 1,
        Active = 2,
        Rejected = 3,
        Lost = 4,
        PageHidden = 5
    }

    internal static class DeucarianPointerCapturePlatformBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DeucarianPointerCaptureRequest();

        [DllImport("__Internal")]
        private static extern void DeucarianPointerCaptureRelease();

        [DllImport("__Internal")]
        private static extern int DeucarianPointerCaptureGetStatus();

        [DllImport("__Internal")]
        private static extern void DeucarianPointerCaptureSetReleaseOnPageHidden(int enabled);
#endif

        public static DeucarianPointerCapturePlatform CurrentPlatform
        {
            get
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

        public static bool IsSupported =>
            CurrentPlatform != DeucarianPointerCapturePlatform.Unsupported;

        public static void Initialize(bool releaseOnPageHidden)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Keep Unity's lock state synchronized with the browser. In particular,
            // releasing browser pointer lock must not leave Unity in its centered,
            // sticky locked state or silently re-lock on the next canvas interaction.
            WebGLInput.stickyCursorLock = false;
            DeucarianPointerCaptureSetReleaseOnPageHidden(releaseOnPageHidden ? 1 : 0);
#endif
        }

        public static DeucarianPointerCapturePlatformStatus Request(bool hideCursor)
        {
            if (!IsSupported)
            {
                return DeucarianPointerCapturePlatformStatus.Rejected;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            DeucarianPointerCaptureRequest();
            if (hideCursor)
            {
                Cursor.visible = false;
            }

            return Observe();
#else
            Cursor.lockState = CursorLockMode.Locked;
            if (hideCursor)
            {
                Cursor.visible = false;
            }

            return Cursor.lockState == CursorLockMode.Locked
                ? DeucarianPointerCapturePlatformStatus.Active
                : DeucarianPointerCapturePlatformStatus.Rejected;
#endif
        }

        public static DeucarianPointerCapturePlatformStatus Observe()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            int status = DeucarianPointerCaptureGetStatus();
            return status >= (int)DeucarianPointerCapturePlatformStatus.Idle &&
                   status <= (int)DeucarianPointerCapturePlatformStatus.PageHidden
                ? (DeucarianPointerCapturePlatformStatus)status
                : DeucarianPointerCapturePlatformStatus.Lost;
#else
            return Cursor.lockState == CursorLockMode.Locked
                ? DeucarianPointerCapturePlatformStatus.Active
                : DeucarianPointerCapturePlatformStatus.Idle;
#endif
        }

        public static void Release(CursorLockMode restoreLockMode, bool restoreVisibility)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            DeucarianPointerCaptureRelease();
#endif
            Cursor.lockState = restoreLockMode;
            Cursor.visible = restoreVisibility;
        }
    }
}
