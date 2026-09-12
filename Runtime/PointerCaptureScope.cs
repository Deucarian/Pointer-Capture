using System;
using Deucarian.Common;
using UnityEngine;

namespace Deucarian.PointerCapture
{
    /// <summary>Application-owned capture lifetime. Create once at startup; lend sessions to viewers.</summary>
    public sealed class PointerCaptureScope : IDisposable
    {
        private GameObject host;
        private DeucarianPointerCaptureController controller;
        public PointerCaptureScope()
        {
            host = new GameObject("Pointer Capture (application scope)") { hideFlags = HideFlags.DontSave };
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(host);
            controller = host.AddComponent<DeucarianPointerCaptureController>();
            controller.FollowProjectDefaults();
        }
        public IPointerCaptureSession OpenSession(object owner = null)
        {
            if (controller == null) throw new ObjectDisposedException(nameof(PointerCaptureScope));
            return controller.OpenSession(owner);
        }
        public void Dispose()
        {
            if (host == null) return;
            controller.Service.Dispose();
            host.SetActive(false);
            UnityObjectUtility.DestroySafely(host);
            host = null; controller = null;
        }
    }

    /// <summary>Preserves old scene wiring. New applications lend a session from their application scope.</summary>
    public static class PointerCaptureCompatibility
    {
        public static IPointerCaptureSession OpenSceneSession(Component owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var controller = owner.GetComponent<DeucarianPointerCaptureController>();
            if (controller == null) controller = owner.gameObject.AddComponent<DeucarianPointerCaptureController>();
            return controller.OpenSession(owner);
        }
    }
}
