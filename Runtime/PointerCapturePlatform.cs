using UnityEngine;

namespace Deucarian.PointerCapture
{
    internal interface IPointerCapturePlatform
    {
        DeucarianPointerCapturePlatform CurrentPlatform { get; }
        bool IsSupported { get; }
        bool HasFocus { get; }
        CursorLockMode LockState { get; }
        bool CursorVisible { get; }
        void Initialize(bool releaseOnPageHidden);
        DeucarianPointerCapturePlatformStatus Request(bool hideCursor);
        DeucarianPointerCapturePlatformStatus Observe();
        bool TryGetPointerPosition(out Vector2 position);
        void Release(CursorLockMode lockMode, bool visible, bool restorePosition, Vector2 position);
    }

    internal sealed class PointerCapturePlatform : IPointerCapturePlatform
    {
        public DeucarianPointerCapturePlatform CurrentPlatform => DeucarianPointerCapturePlatformBridge.CurrentPlatform;
        public bool IsSupported => DeucarianPointerCapturePlatformBridge.IsSupported;
        public bool HasFocus => Application.isFocused;
        public CursorLockMode LockState => Cursor.lockState;
        public bool CursorVisible => Cursor.visible;
        public void Initialize(bool releaseOnPageHidden) => DeucarianPointerCapturePlatformBridge.Initialize(releaseOnPageHidden);
        public DeucarianPointerCapturePlatformStatus Request(bool hideCursor) => DeucarianPointerCapturePlatformBridge.Request(hideCursor);
        public DeucarianPointerCapturePlatformStatus Observe() => DeucarianPointerCapturePlatformBridge.Observe();
        public bool TryGetPointerPosition(out Vector2 position) => DeucarianPointerCapturePlatformBridge.TryGetPointerPosition(out position);
        public void Release(CursorLockMode lockMode, bool visible, bool restorePosition, Vector2 position) =>
            DeucarianPointerCapturePlatformBridge.Release(lockMode, visible, restorePosition, position);
    }
}
